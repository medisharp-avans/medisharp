using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Server.Exceptions;
using Server.Models.DTO.Requests;
using Server.Models.Enums;
using Server.Repositories;
using Server.Repositories.InterFaces;
using Server.Tests.TestDb;

namespace Server.Tests.Integration
{
    /// <summary>
    /// Integratietests voor CreateWithReferralAsync.
    /// HasOverlapAsync en IsWithinAvailabilityAsync zitten niet meer in AppointmentRepository;
    /// die worden getest in PlanningRepositoryAvailabilityTests.
    /// Gebruikt de seed: specialist 1 (KNO), patiënt 2 en huisarts 1.
    /// </summary>
    [TestClass]
    public class AppointmentRepositoryPlanningTests : DatabaseTestBase
    {
        private static readonly DateTime Start = new(2027, 1, 12, 10, 0, 0);
        private static readonly DateTime End = new(2027, 1, 12, 11, 0, 0);

        private IAppointmentRepository _sut = null!;

        protected override Func<DbContext, Task>? GetTestDataInitializer()
        {
            return TestAFSDataInitializer.SeedAsync;
        }

        [TestInitialize]
        public async Task CreateSut()
        {
            _sut = new AppointmentRepository(DbContext);
        }

        private static CreateAppointmentDTO AppointmentDto(int specialistId = 1) => new()
        {
            PatientId = 2,
            SpecialistId = specialistId,
            StartTime = Start,
            EndTime = End
        };

        private static CreateReferralRequestDTO ReferralDto() => new()
        {
            PatientId = 2,
            GeneralPractitionerId = 1,
            Specialism = Specialism.KNO,
            Reason = "Oorpijn",
            Urgency = ReferralUrgency.Regulier,
            TreatmentCode = "KNO002"
        };

        private static async Task<int> CountReferralsAsync() =>
            Convert.ToInt32((await TestDatabase.QueryAsync("SELECT COUNT(*) FROM Referrals")).Rows[0][0]);

        [TestMethod]
        public async Task CreateWithReferralAsync_ValidDtos_StoresAppointmentLinkedToNewReferral()
        {
            var referralsBefore = await CountReferralsAsync();

            var result = await _sut.CreateWithReferralAsync(AppointmentDto(), ReferralDto());

            Assert.IsTrue(result.Id > 0);
            Assert.AreEqual(referralsBefore + 1, await CountReferralsAsync());

            var rows = await TestDatabase.QueryAsync(@"
                SELECT r.Status, r.TreatmentCode, r.PatientId
                FROM Appointments a JOIN Referrals r ON r.Id = a.ReferralId
                WHERE a.Id = @id", new SqlParameter("@id", result.Id));

            Assert.AreEqual(1, rows.Rows.Count);
            Assert.AreEqual("Ingepland", rows.Rows[0]["Status"]);
            Assert.AreEqual("KNO002", rows.Rows[0]["TreatmentCode"]);
            Assert.AreEqual(2, rows.Rows[0]["PatientId"]);
        }

        [TestMethod]
        public async Task CreateWithReferralAsync_ValidDtos_StoresAppointmentSpecialistAndTimes()
        {
            var result = await _sut.CreateWithReferralAsync(AppointmentDto(), ReferralDto());

            var rows = await TestDatabase.QueryAsync(
                "SELECT SpecialistId, StartTime, EndTime FROM Appointments WHERE Id = @id",
                new SqlParameter("@id", result.Id));

            Assert.AreEqual(1, rows.Rows[0]["SpecialistId"]);
            Assert.AreEqual(Start, rows.Rows[0]["StartTime"]);
            Assert.AreEqual(End, rows.Rows[0]["EndTime"]);
        }

        [TestMethod]
        public async Task CreateWithReferralAsync_SameSlotTwice_ThrowsSlotUnavailableAndCreatesNoReferral()
        {
            await _sut.CreateWithReferralAsync(AppointmentDto(), ReferralDto());
            var referralsBefore = await CountReferralsAsync();

            // Verse context: de mislukte entiteiten van een eerdere poging blijven anders getrackt
            await using var context = TestDatabase.CreateDbContext();
            var second = new AppointmentRepository(context);

            var exception = await Assert.ThrowsAsync<SlotUnavailableException>(
                () => second.CreateWithReferralAsync(AppointmentDto(), ReferralDto()));

            StringAssert.Contains(exception.Message, "Dit tijdslot is niet meer beschikbaar");
            Assert.AreEqual(referralsBefore, await CountReferralsAsync(), "Er is een wees-verwijzing achtergebleven.");
        }
    }
}