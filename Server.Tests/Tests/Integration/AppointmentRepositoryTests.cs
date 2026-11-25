using Microsoft.Data.SqlClient;
using Server.Exceptions;
using Server.Models.DTO.Requests;
using Server.Models.Enums;
using Server.Repositories;
using Server.Repositories.InterFaces;
using Server.Tests.TestDb;

namespace Server.Tests.Integration
{
    /// <summary>
    /// Integratietests voor CreateWithReferralAsync, HasOverlapAsync en IsWithinAvailabilityAsync.
    /// Gebruikt de seed: specialist 1 (KNO) is op elke dag van januari 2027 beschikbaar van 08:00 tot 17:00.
    /// </summary>
    [TestClass]
    public class AppointmentRepositoryPlanningTests : DatabaseTestBase
    {
        private static readonly DateTime Start = new(2027, 1, 12, 10, 0, 0);
        private static readonly DateTime End = new(2027, 1, 12, 11, 0, 0);

        private IAppointmentRepository _sut = null!;

        [TestInitialize]
        public async Task CreateSut()
        {
            _sut = new AppointmentRepository(DbContext);
            await TestDatabase.ResetAsync(TestAFSDataInitializer.SeedAsync);
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
            Urgency = "Regulier",
            TreatmentCode = "KNO002"
        };

        private static async Task<int> CountReferralsAsync() =>
            Convert.ToInt32((await TestDatabase.QueryAsync("SELECT COUNT(*) FROM Referrals")).Rows[0][0]);

        // ---------- CreateWithReferralAsync ----------

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
        public async Task CreateWithReferralAsync_SameSlotTwice_ThrowsSlotUnavailableAndCreatesNoReferral()
        {
            await _sut.CreateWithReferralAsync(AppointmentDto(), ReferralDto());
            var referralsBefore = await CountReferralsAsync();

            // Verse context: de mislukte entiteiten van een eerdere poging blijven anders getrackt
            await using var context = TestDatabase.CreateDbContext();
            var second = new AppointmentRepository(context);

            await Assert.ThrowsAsync<SlotUnavailableException>(
                () => second.CreateWithReferralAsync(AppointmentDto(), ReferralDto()));

            Assert.AreEqual(referralsBefore, await CountReferralsAsync(), "Er is een wees-verwijzing achtergebleven.");
        }

        // ---------- HasOverlapAsync ----------

        //[TestMethod]
        //public async Task HasOverlapAsync_ExistingAppointment_DetectsOverlapButNotAdjacentSlots()
        //{
        //    await _sut.CreateWithReferralAsync(AppointmentDto(), ReferralDto());

        //    Assert.IsTrue(await _sut.HasOverlapAsync(1, Start, End), "Zelfde tijdslot");
        //    Assert.IsTrue(await _sut.HasOverlapAsync(1, Start.AddMinutes(30), End.AddMinutes(30)), "Half overlappend");
        //    Assert.IsFalse(await _sut.HasOverlapAsync(1, End, End.AddHours(1)), "Direct erna");
        //    Assert.IsFalse(await _sut.HasOverlapAsync(1, Start.AddHours(-1), Start), "Direct ervoor");
        //    Assert.IsFalse(await _sut.HasOverlapAsync(2, Start, End), "Andere specialist");
        //}

        //// ---------- IsWithinAvailabilityAsync ----------

        //[TestMethod]
        //public async Task IsWithinAvailabilityAsync_InsideBlock_ReturnsTrue()
        //{
        //    Assert.IsTrue(await _sut.IsWithinAvailabilityAsync(1, Start, End));
        //    Assert.IsTrue(await _sut.IsWithinAvailabilityAsync(
        //        1, new DateTime(2027, 1, 12, 8, 0, 0), new DateTime(2027, 1, 12, 9, 0, 0)), "Begin van het blok");
        //    Assert.IsTrue(await _sut.IsWithinAvailabilityAsync(
        //        1, new DateTime(2027, 1, 12, 16, 0, 0), new DateTime(2027, 1, 12, 17, 0, 0)), "Einde van het blok");
        //}

        //[TestMethod]
        //public async Task IsWithinAvailabilityAsync_OutsideBlock_ReturnsFalse()
        //{
        //    Assert.IsFalse(await _sut.IsWithinAvailabilityAsync(
        //        1, new DateTime(2027, 1, 12, 7, 0, 0), new DateTime(2027, 1, 12, 8, 0, 0)), "Voor het blok");
        //    Assert.IsFalse(await _sut.IsWithinAvailabilityAsync(
        //        1, new DateTime(2027, 1, 12, 16, 30, 0), new DateTime(2027, 1, 12, 17, 30, 0)), "Steekt over het einde");
        //    Assert.IsFalse(await _sut.IsWithinAvailabilityAsync(
        //        1, new DateTime(2027, 2, 1, 10, 0, 0), new DateTime(2027, 2, 1, 11, 0, 0)), "Dag zonder beschikbaarheid");
        //}

        //[TestMethod]
        //public async Task IsWithinAvailabilityAsync_AfterSetUnavailable_ReturnsFalse()
        //{
        //    await TestDatabase.ExecuteAsync(
        //        "DELETE FROM Availabilities WHERE SpecialistId = 1 AND CAST(StartTime AS date) = '2027-01-12'");

        //    Assert.IsFalse(await _sut.IsWithinAvailabilityAsync(1, Start, End));
        //}
    }
}