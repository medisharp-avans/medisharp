using Moq;
using Server.Exceptions;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Repositories.InterFaces;
using Server.Services;
using Server.Services.InterFaces;

namespace Server.Tests.Tests.Unit
{
    /// <summary>
    /// Dekt de controles die eerst in AppointmentService zaten: beschikbaarheid en overlap,
    /// nu met de keuze voor de voorkeursspecialist en de terugval op de eerste beschikbare specialist.
    /// Het tijdstip ligt voorlopig vast op morgen 10:00 (TODO in PlanningService); pas _start aan
    /// zodra dat verandert.
    /// </summary>
    [TestClass]
    public class PlanningServiceTests
    {
        private Mock<IPlanningRepository> _repositoryMock = null!;
        private Mock<ICalculateService> _calculateServiceMock = null!;
        private PlanningService _sut = null!;

        private DateTime _start;
        private DateTime _end;

        [TestInitialize]
        public void Setup()
        {
            _start = DateTime.Today.AddDays(1).AddHours(10);
            _end = _start.AddHours(1);

            _repositoryMock = new Mock<IPlanningRepository>();
            _calculateServiceMock = new Mock<ICalculateService>();
            _sut = new PlanningService(_repositoryMock.Object, _calculateServiceMock.Object);

            _calculateServiceMock
                .Setup(c => c.CalculateAppointmentEndTime(_start, "KNO002"))
                .ReturnsAsync(_end);
        }

        private static AppointmentManualRequestDTO Dto(int? preferredSpecialistId = null) => new()
        {
            PatientId = 1,
            Specialism = Specialism.KNO,
            PreferredSpecialistId = preferredSpecialistId,
            Reason = "Oorpijn",
            Urgency = "Regulier",
            TreatmentCode = "KNO002"
        };

        private void SetupSpecialist(int id, bool available, bool overlap = false)
        {
            _repositoryMock
                .Setup(r => r.IsWithinAvailabilityAsync(id, _start, _end))
                .ReturnsAsync(available);
            _repositoryMock
                .Setup(r => r.HasOverlapAsync(id, _start, _end))
                .ReturnsAsync(overlap);
        }

        private void SetupSpecialistsOfSpecialism(params int?[] ids) =>
            _repositoryMock
                .Setup(r => r.GetSpecialistsBySpecialism("KNO"))
                .ReturnsAsync([.. ids.Select(id => new PlanningResponseDTO { SpecialistId = id })]);

        [TestMethod]
        public async Task SetSpecialistForAppointment_PreferredSpecialistAvailable_ReturnsPreferredSpecialist()
        {
            SetupSpecialist(2, available: true);

            var result = await _sut.SetSpecialistForAppointment(Dto(preferredSpecialistId: 2));

            Assert.AreEqual(2, result.SpecialistId);
            Assert.AreEqual(_start, result.StartTime);
            Assert.AreEqual(_end, result.EndTime);

            // De voorkeur is beschikbaar, dus de rest van het specialisme hoeft niet bekeken te worden
            _repositoryMock.Verify(r => r.GetSpecialistsBySpecialism(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_ValidDto_CalculatesEndTimeFromTreatmentCode()
        {
            SetupSpecialist(2, available: true);

            await _sut.SetSpecialistForAppointment(Dto(preferredSpecialistId: 2));

            _calculateServiceMock.Verify(c => c.CalculateAppointmentEndTime(_start, "KNO002"), Times.Once);
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_PreferredSpecialistHasOverlap_FallsBackToFirstAvailableSpecialist()
        {
            SetupSpecialist(2, available: true, overlap: true);
            SetupSpecialistsOfSpecialism(3, 4);
            SetupSpecialist(3, available: false);
            SetupSpecialist(4, available: true);

            var result = await _sut.SetSpecialistForAppointment(Dto(preferredSpecialistId: 2));

            Assert.AreEqual(4, result.SpecialistId);
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_PreferredSpecialistNotAvailable_FallsBackToFirstAvailableSpecialist()
        {
            SetupSpecialist(2, available: false);
            SetupSpecialistsOfSpecialism(3);
            SetupSpecialist(3, available: true);

            var result = await _sut.SetSpecialistForAppointment(Dto(preferredSpecialistId: 2));

            Assert.AreEqual(3, result.SpecialistId);
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_NoPreference_ReturnsFirstAvailableSpecialistOfSpecialism()
        {
            SetupSpecialistsOfSpecialism(3, 4);
            SetupSpecialist(3, available: true);
            SetupSpecialist(4, available: true);

            var result = await _sut.SetSpecialistForAppointment(Dto());

            Assert.AreEqual(3, result.SpecialistId);
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_SpecialistWithoutId_IsSkipped()
        {
            SetupSpecialistsOfSpecialism(null, 4);
            SetupSpecialist(4, available: true);

            var result = await _sut.SetSpecialistForAppointment(Dto());

            Assert.AreEqual(4, result.SpecialistId);
        }

        [TestMethod]
        public async Task SetSpecialistForAppointment_NobodyAvailable_ThrowsSlotUnavailable()
        {
            SetupSpecialist(2, available: true, overlap: true);
            SetupSpecialistsOfSpecialism(3, 4);
            SetupSpecialist(3, available: false);
            SetupSpecialist(4, available: true, overlap: true);

            var exception = await Assert.ThrowsAsync<SlotUnavailableException>(
                () => _sut.SetSpecialistForAppointment(Dto(preferredSpecialistId: 2)));

            Assert.Contains("Geen beschikbare specialist gevonden", exception.Message);
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