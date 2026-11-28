using Microsoft.EntityFrameworkCore;
using Server.Models;
using Server.Models.DTO.Requests;
using Server.Models.Enums;
using Server.Repositories;
using Server.Repositories.InterFaces;
using Server.Tests.TestDb;

namespace Server.Tests.Tests.Integration
{
    /// <summary>
    /// Integratietests voor HasOverlapAsync en GetBusyIntervalsAsync van PlanningRepository.
    /// Gebruikt de seed: specialist 1 (KNO), specialist 2 (KNO), patiënt 2 en huisarts 1.
    /// </summary>
    [TestClass]
    public class PlanningRepositoryAvailabilityTests : DatabaseTestBase
    {
        private static readonly DateTime Start = new(2027, 1, 12, 10, 0, 0);
        private static readonly DateTime End = new(2027, 1, 12, 11, 0, 0);

        private AppointmentRepository _appointments = null!;
        private PlanningRepository _sut = null!;

        protected override Func<DbContext, Task>? GetTestDataInitializer()
        {
            return TestAFSDataInitializer.SeedAsync;
        }

        [TestInitialize]
        public async Task CreateSut()
        {
            _appointments = new AppointmentRepository(DbContext);
            _sut = new PlanningRepository(DbContext);
            await Task.CompletedTask;
        }

        /// <summary>Specialist 1 heeft een afspraak van 10:00 tot 11:00.</summary>
        private async Task<Appointment> ArrangeAppointmentAsync() =>
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
                    Urgency = ReferralUrgency.Regulier,
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

        // ---------- GetBusyIntervalsAsync ----------

        [TestMethod]
        public async Task GetBusyIntervalsAsync_CombinesAppointmentsAndExternalEventsInLocalTime()
        {
            // Arrange: Interne afspraak 10:00 - 11:00 (lokale tijd)
            await ArrangeAppointmentAsync();

            // Extern iCal event op 12 jan 2027 (wintertijd CET = UTC+1):
            // 13:00 - 14:30 UTC moet 14:00 - 15:30 lokale tijd worden
            DbContext.ExternalCalendarEvents.Add(new ExternalCalendarEvent
            {
                SpecialistId = 1,
                ExternalUid = "ical-event-1",
                Title = "Privé afspraak",
                StartUtc = new DateTime(2027, 1, 12, 13, 0, 0, DateTimeKind.Utc),
                EndUtc = new DateTime(2027, 1, 12, 14, 30, 0, DateTimeKind.Utc),
                IsAllDay = false
            });
            await DbContext.SaveChangesAsync();

            var windowStart = new DateTime(2027, 1, 12, 0, 0, 0);
            var windowEnd = new DateTime(2027, 1, 19, 0, 0, 0);

            // Act
            var intervals = await _sut.GetBusyIntervalsAsync(1, windowStart, windowEnd);

            // Assert
            Assert.AreEqual(2, intervals.Count);
            Assert.IsTrue(intervals.Any(i => i.Start == Start && i.End == End), "Interne afspraak ontbreekt");
            Assert.IsTrue(intervals.Any(i =>
                i.Start == new DateTime(2027, 1, 12, 14, 0, 0) &&
                i.End == new DateTime(2027, 1, 12, 15, 30, 0)), "Extern event niet goed omgerekend naar Nederlandse tijd");
        }

        [TestMethod]
        public async Task GetBusyIntervalsAsync_IgnoresCancelledAppointmentsAndOtherSpecialists()
        {
            // Arrange: Geannuleerde afspraak voor specialist 1 + extern event voor specialist 2
            await ArrangeAppointmentAsync();
            var cancelled = await DbContext.Appointments.FirstAsync(a => a.SpecialistId == 1);
            cancelled.Status = AppointmentStatus.Geannuleerd;

            DbContext.ExternalCalendarEvents.Add(new ExternalCalendarEvent
            {
                SpecialistId = 2,
                ExternalUid = "ical-other-specialist",
                Title = "Andere arts",
                StartUtc = new DateTime(2027, 1, 12, 10, 0, 0, DateTimeKind.Utc),
                EndUtc = new DateTime(2027, 1, 12, 11, 0, 0, DateTimeKind.Utc)
            });
            await DbContext.SaveChangesAsync();

            var windowStart = new DateTime(2027, 1, 12, 0, 0, 0);
            var windowEnd = new DateTime(2027, 1, 19, 0, 0, 0);

            // Act
            var intervals = await _sut.GetBusyIntervalsAsync(1, windowStart, windowEnd);

            // Assert
            Assert.AreEqual(0, intervals.Count);
        }
    }
}
