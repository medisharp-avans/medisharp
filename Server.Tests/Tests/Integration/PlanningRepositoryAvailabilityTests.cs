using Microsoft.EntityFrameworkCore;
using Server.Models.DTO.Requests;
using Server.Models.Enums;
using Server.Repositories;
using Server.Repositories.InterFaces;
using Server.Tests.TestDb;

namespace Server.Tests.Integration
{
    /// <summary>
    /// Integratietests voor HasOverlapAsync en IsWithinAvailabilityAsync van PlanningRepository
    /// (verhuisd uit AppointmentRepositoryPlanningTests).
    /// Gebruikt de seed: specialist 1 (KNO) is op elke dag van januari 2027 beschikbaar van 08:00 tot 17:00.
    /// </summary>
    [TestClass]
    public class PlanningRepositoryAvailabilityTests : DatabaseTestBase
    {
        private static readonly DateTime Start = new(2027, 1, 12, 10, 0, 0);
        private static readonly DateTime End = new(2027, 1, 12, 11, 0, 0);

        private IAppointmentRepository _appointments = null!;
        private IPlanningRepository _sut = null!;

        protected override Func<DbContext, Task>? GetTestDataInitializer()
        {
            return TestAFSDataInitializer.SeedAsync;
        }

        [TestInitialize]
        public async Task CreateSut()
        {
            _appointments = new AppointmentRepository(DbContext);
            _sut = new PlanningRepository(DbContext);
        }

        /// <summary>Specialist 1 heeft een afspraak van 10:00 tot 11:00.</summary>
        private async Task ArrangeAppointmentAsync() =>
            await _appointments.CreateWithReferralAsync(
                new CreateAppointmentDTO
                {
                    PatientId = 2,
                    SpecialistId = 1,
                    StartTime = Start,
                    EndTime = End
                },
                new CreateReferralRequestDTO
                {
                    PatientId = 2,
                    GeneralPractitionerId = 1,
                    Specialism = Specialism.KNO,
                    Reason = "Oorpijn",
                    Urgency = "Regulier",
                    TreatmentCode = "KNO002"
                });

        // ---------- HasOverlapAsync ----------

        [TestMethod]
        public async Task HasOverlapAsync_ExistingAppointment_DetectsOverlapButNotAdjacentSlots()
        {
            await ArrangeAppointmentAsync();

            Assert.IsTrue(await _sut.HasOverlapAsync(1, Start, End), "Zelfde tijdslot");
            Assert.IsTrue(await _sut.HasOverlapAsync(1, Start.AddMinutes(30), End.AddMinutes(30)), "Half overlappend");
            Assert.IsFalse(await _sut.HasOverlapAsync(1, End, End.AddHours(1)), "Direct erna");
            Assert.IsFalse(await _sut.HasOverlapAsync(1, Start.AddHours(-1), Start), "Direct ervoor");
            Assert.IsFalse(await _sut.HasOverlapAsync(2, Start, End), "Andere specialist");
        }

        [TestMethod]
        public async Task HasOverlapAsync_NoAppointments_ReturnsFalse()
        {
            Assert.IsFalse(await _sut.HasOverlapAsync(1, Start, End));
        }

        // ---------- IsWithinAvailabilityAsync ----------

        [TestMethod]
        public async Task IsWithinAvailabilityAsync_InsideBlock_ReturnsTrue()
        {
            Assert.IsTrue(await _sut.IsWithinAvailabilityAsync(1, Start, End));
            Assert.IsTrue(await _sut.IsWithinAvailabilityAsync(
                1, new DateTime(2027, 1, 12, 8, 0, 0), new DateTime(2027, 1, 12, 9, 0, 0)), "Begin van het blok");
            Assert.IsTrue(await _sut.IsWithinAvailabilityAsync(
                1, new DateTime(2027, 1, 12, 16, 0, 0), new DateTime(2027, 1, 12, 17, 0, 0)), "Einde van het blok");
        }

        [TestMethod]
        public async Task IsWithinAvailabilityAsync_OutsideBlock_ReturnsFalse()
        {
            Assert.IsFalse(await _sut.IsWithinAvailabilityAsync(
                1, new DateTime(2027, 1, 12, 7, 0, 0), new DateTime(2027, 1, 12, 8, 0, 0)), "Voor het blok");
            Assert.IsFalse(await _sut.IsWithinAvailabilityAsync(
                1, new DateTime(2027, 1, 12, 16, 30, 0), new DateTime(2027, 1, 12, 17, 30, 0)), "Steekt over het einde");
            Assert.IsFalse(await _sut.IsWithinAvailabilityAsync(
                1, new DateTime(2027, 2, 1, 10, 0, 0), new DateTime(2027, 2, 1, 11, 0, 0)), "Dag zonder beschikbaarheid");
        }

        [TestMethod]
        public async Task IsWithinAvailabilityAsync_AfterSetUnavailable_ReturnsFalse()
        {
            await TestDatabase.ExecuteAsync(
                "DELETE FROM Availabilities WHERE SpecialistId = 1 AND CAST(StartTime AS date) = '2027-01-12'");

            Assert.IsFalse(await _sut.IsWithinAvailabilityAsync(1, Start, End));
        }
    }
}