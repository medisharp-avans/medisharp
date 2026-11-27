using Moq;
using Server.Exceptions;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Repositories.InterFaces;
using Server.Services;
using Server.Services.InterFaces;

namespace Server.Tests.Unit
{
    /// <summary>
    /// Unit tests for the 3-week automatic scheduling algorithm in PlanningService:
    /// validates workday/working-hour boundaries, 15-minute slot stepping, end-time overlap checks,
    /// preferred specialist priority, and fallback to other specialists in the same specialism.
    /// </summary>
    [TestClass]
    public class PlanningServiceTests
    {
        private static readonly TimeZoneInfo DutchTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        private Mock<IPlanningRepository> _repositoryMock = null!;
        private Mock<ICalculateService> _calculateServiceMock = null!;
        private PlanningService _sut = null!;

        private DateTime _windowStart;
        private DateTime _windowEnd;
        private DateTime _firstWorkdayAtNine;

        [TestInitialize]
        public void Setup()
        {
            var dutchNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, DutchTimeZone);
            _windowStart = dutchNow.Date.AddDays(1);
            _windowEnd = _windowStart.AddDays(21);

            // Find the first Monday-Friday day inside the 21-day window so tests pass regardless of the day they run
            var firstWorkday = _windowStart;
            while (firstWorkday.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                firstWorkday = firstWorkday.AddDays(1);
            }
            _firstWorkdayAtNine = firstWorkday.AddHours(9);

            _repositoryMock = new Mock<IPlanningRepository>();
            _calculateServiceMock = new Mock<ICalculateService>();
            _sut = new PlanningService(_repositoryMock.Object, _calculateServiceMock.Object);

            // Default treatment duration: 1 hour (4 quarters)
            _calculateServiceMock
                .Setup(c => c.CalculateAppointmentEndTime(It.IsAny<DateTime>(), "KNO002"))
                .ReturnsAsync((DateTime start, string _) => start.AddHours(1));

            // Default: no other specialists unless configured in the test
            SetupSpecialistsOfSpecialism();
        }

        private static AppointmentManualRequestDTO Dto(int? preferredSpecialistId = null) => new()
        {
            PatientId = 1,
            Specialism = Specialism.KNO,
            PreferredSpecialistId = preferredSpecialistId,
            Reason = "Oorpijn",
            Urgency = ReferralUrgency.Regulier,
            TreatmentCode = "KNO002"
        };

        private void SetupBusyIntervals(int specialistId, params (DateTime Start, DateTime End)[] intervals) =>
            _repositoryMock
                .Setup(r => r.GetBusyIntervalsAsync(specialistId, _windowStart, _windowEnd))
                .ReturnsAsync(intervals.ToList());

        private void SetupSpecialistsOfSpecialism(params int?[] ids) =>
            _repositoryMock
                .Setup(r => r.GetSpecialistsBySpecialism("KNO"))
                .ReturnsAsync(ids.Select(id => new PlanningResponseDTO { SpecialistId = id }).ToList());

        private static DateTime NextWorkday(DateTime date)
        {
            var next = date.Date.AddDays(1);
            while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                next = next.AddDays(1);
            }
            return next;
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_PreferredSpecialistCompletelyFree_SchedulesFirstWorkdayAtNine()
        {
            SetupBusyIntervals(2);

            var result = await _sut.SetSpecialistForAppointment(Dto(preferredSpecialistId: 2));

            Assert.AreEqual(2, result.SpecialistId);
            Assert.AreEqual(_firstWorkdayAtNine, result.StartTime);
            Assert.AreEqual(_firstWorkdayAtNine.AddHours(1), result.EndTime);

            // Preferred specialist had a slot, so no busy intervals needed to be fetched for other specialists
            _repositoryMock.Verify(r => r.GetBusyIntervalsAsync(2, _windowStart, _windowEnd), Times.Once);
            _repositoryMock.Verify(r => r.GetBusyIntervalsAsync(It.Is<int>(id => id != 2), It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_ValidDto_CalculatesDurationFromTreatmentCode()
        {
            SetupBusyIntervals(2);

            await _sut.SetSpecialistForAppointment(Dto(preferredSpecialistId: 2));

            _calculateServiceMock.Verify(c => c.CalculateAppointmentEndTime(It.IsAny<DateTime>(), "KNO002"), Times.Once);
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_EndTimeSpillsOverIntoLaterEvent_SkipsToSlotAfterEvent()
        {
            // 09:00 - 09:30 looks free, but a 1-hour appointment (09:00 - 10:00) would overlap with 09:30 - 10:15.
            // The algorithm must skip 09:00, 09:15, 09:30, 09:45, 10:00 and schedule at 10:15 - 11:15.
            SetupBusyIntervals(2, (_firstWorkdayAtNine.AddMinutes(30), _firstWorkdayAtNine.AddMinutes(75)));

            var result = await _sut.SetSpecialistForAppointment(Dto(preferredSpecialistId: 2));

            Assert.AreEqual(2, result.SpecialistId);
            Assert.AreEqual(_firstWorkdayAtNine.AddMinutes(75), result.StartTime); // 10:15
            Assert.AreEqual(_firstWorkdayAtNine.AddMinutes(135), result.EndTime);  // 11:15
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_RemainingDayTooShortBeforeFivePm_MovesToNextWorkdayAtNine()
        {
            // Busy from 09:00 until 16:15 -> only 45 mins left before 17:00, not enough for a 1-hour treatment.
            SetupBusyIntervals(2, (_firstWorkdayAtNine, _firstWorkdayAtNine.Date.AddHours(16).AddMinutes(15)));

            var result = await _sut.SetSpecialistForAppointment(Dto(preferredSpecialistId: 2));

            var expectedNextWorkday = NextWorkday(_firstWorkdayAtNine).AddHours(9);
            Assert.AreEqual(expectedNextWorkday, result.StartTime);
            Assert.AreEqual(expectedNextWorkday.AddHours(1), result.EndTime);
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_PreferredSpecialistFullForThreeWeeks_FallsBackToAvailableSpecialist()
        {
            // Preferred specialist 2 is completely blocked for the entire 21-day window
            SetupBusyIntervals(2, (_windowStart, _windowEnd));
            SetupSpecialistsOfSpecialism(2, 3, 4);
            SetupBusyIntervals(3, (_windowStart, _windowEnd));
            SetupBusyIntervals(4); // Specialist 4 is free

            var result = await _sut.SetSpecialistForAppointment(Dto(preferredSpecialistId: 2));

            Assert.AreEqual(4, result.SpecialistId);
            Assert.AreEqual(_firstWorkdayAtNine, result.StartTime);
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_NoPreference_ReturnsFirstAvailableSpecialistOfSpecialism()
        {
            SetupSpecialistsOfSpecialism(3, 4);
            SetupBusyIntervals(3);
            SetupBusyIntervals(4);

            var result = await _sut.SetSpecialistForAppointment(Dto());

            Assert.AreEqual(3, result.SpecialistId);
            Assert.AreEqual(_firstWorkdayAtNine, result.StartTime);
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_SpecialistWithoutId_IsSkipped()
        {
            SetupSpecialistsOfSpecialism(null, 4);
            SetupBusyIntervals(4);

            var result = await _sut.SetSpecialistForAppointment(Dto());

            Assert.AreEqual(4, result.SpecialistId);
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_NobodyAvailableInThreeWeeks_ThrowsSlotUnavailable()
        {
            SetupBusyIntervals(2, (_windowStart, _windowEnd));
            SetupSpecialistsOfSpecialism(3, 4);
            SetupBusyIntervals(3, (_windowStart, _windowEnd));
            SetupBusyIntervals(4, (_windowStart, _windowEnd));

            var exception = await Assert.ThrowsAsync<SlotUnavailableException>(
                () => _sut.SetSpecialistForAppointment(Dto(preferredSpecialistId: 2)));

            StringAssert.Contains(exception.Message, "Geen beschikbare specialist gevonden");
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_NoSpecialistsInSpecialism_ThrowsSlotUnavailable()
        {
            SetupSpecialistsOfSpecialism();

            await Assert.ThrowsAsync<SlotUnavailableException>(
                () => _sut.SetSpecialistForAppointment(Dto()));
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_UnknownTreatmentCode_ThrowsAndChecksNothing()
        {
            _calculateServiceMock
                .Setup(c => c.CalculateAppointmentEndTime(It.IsAny<DateTime>(), "ONBEKEND"))
                .ThrowsAsync(new KeyNotFoundException("Zorgcode ONBEKEND bestaat niet."));
            var dto = Dto(preferredSpecialistId: 2);
            dto.TreatmentCode = "ONBEKEND";

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.SetSpecialistForAppointment(dto));

            _repositoryMock.VerifyNoOtherCalls();
        }
    }
}
