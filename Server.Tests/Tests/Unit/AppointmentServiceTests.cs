using Moq;
using Server.Exceptions;
using Server.Models;
using Server.Models.DTO.Requests;
using Server.Models.Enums;
using Server.Repositories.InterFaces;
using Server.Services;
using Server.Services.InterFaces;

namespace Server.Tests.Unit
{
    [TestClass]
    public class AppointmentServiceTests
    {
        private Mock<IAppointmentRepository> _repositoryMock = null!;
        private Mock<ICalculateService> _calculateServiceMock = null!;
        private AppointmentService _sut = null!;

        private static readonly DateTime Start = new(2027, 1, 12, 10, 0, 0);
        private static readonly DateTime End = new(2027, 1, 12, 11, 0, 0);

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<IAppointmentRepository>();
            _calculateServiceMock = new Mock<ICalculateService>();
            _sut = new AppointmentService(_repositoryMock.Object, _calculateServiceMock.Object);

            // Standaard succespad; individuele tests overschrijven wat nodig is
            _calculateServiceMock
                .Setup(c => c.CalculateAppointmentEndTime(Start, "KNO002"))
                .ReturnsAsync(End);
            _repositoryMock
                .Setup(r => r.IsWithinAvailabilityAsync(2, Start, End))
                .ReturnsAsync(true);
            _repositoryMock
                .Setup(r => r.HasOverlapAsync(2, Start, End))
                .ReturnsAsync(false);
            _repositoryMock
                .Setup(r => r.CreateWithReferralAsync(It.IsAny<CreateAppointmentDTO>(), It.IsAny<CreateReferralRequestDTO>()))
                .ReturnsAsync(new Appointment { Id = 7 });
        }

        private static AppointmentManualRequestDTO CreateValidDto() => new()
        {
            PatientId = 1,
            SpecialistId = 2,
            StartTime = Start,
            GeneralPractitionerId = 3,
            Specialism = Specialism.KNO,
            PreferredSpecialistId = 2,
            Reason = "Oorpijn",
            Questionnaire = "Vragenlijst",
            Urgency = "Regulier",
            TreatmentCode = "KNO002"
        };

        [TestMethod]
        public async Task CreateAppointment_ValidDto_ReturnsAppointmentFromRepository()
        {
            var result = await _sut.CreateAppointment(CreateValidDto());

            Assert.AreEqual(7, result.Id);
        }

        [TestMethod]
        public async Task CreateAppointment_ValidDto_PassesCalculatedEndTimeAndAppointmentFields()
        {
            await _sut.CreateAppointment(CreateValidDto());

            _repositoryMock.Verify(r => r.CreateWithReferralAsync(
                It.Is<CreateAppointmentDTO>(a =>
                    a.PatientId == 1 && a.SpecialistId == 2 && a.StartTime == Start && a.EndTime == End),
                It.IsAny<CreateReferralRequestDTO>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateAppointment_ValidDto_CreatesReferralWithMappedFields()
        {
            await _sut.CreateAppointment(CreateValidDto());

            _repositoryMock.Verify(r => r.CreateWithReferralAsync(
                It.IsAny<CreateAppointmentDTO>(),
                It.Is<CreateReferralRequestDTO>(d =>
                    d.PatientId == 1
                    && d.GeneralPractitionerId == 3
                    && d.Specialism == Specialism.KNO
                    && d.PreferredSpecialistId == 2
                    && d.Reason == "Oorpijn"
                    && d.Questionnaire == "Vragenlijst"
                    && d.Urgency == "Regulier"
                    && d.TreatmentCode == "KNO002")), Times.Once);
        }

        [TestMethod]
        public async Task CreateAppointment_UnknownTreatmentCode_ThrowsAndCreatesNothing()
        {
            _calculateServiceMock
                .Setup(c => c.CalculateAppointmentEndTime(It.IsAny<DateTime>(), "ONBEKEND"))
                .ThrowsAsync(new KeyNotFoundException("Zorgcode ONBEKEND bestaat niet."));
            var dto = CreateValidDto();
            dto.TreatmentCode = "ONBEKEND";

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.CreateAppointment(dto));

            _repositoryMock.Verify(r => r.CreateWithReferralAsync(
                It.IsAny<CreateAppointmentDTO>(), It.IsAny<CreateReferralRequestDTO>()), Times.Never);
        }

        [TestMethod]
        public async Task CreateAppointment_SpecialistNotAvailable_ThrowsAndCreatesNothing()
        {
            _repositoryMock
                .Setup(r => r.IsWithinAvailabilityAsync(2, Start, End))
                .ReturnsAsync(false);

            await Assert.ThrowsAsync<SlotUnavailableException>(() => _sut.CreateAppointment(CreateValidDto()));

            _repositoryMock.Verify(r => r.CreateWithReferralAsync(
                It.IsAny<CreateAppointmentDTO>(), It.IsAny<CreateReferralRequestDTO>()), Times.Never);
        }

        [TestMethod]
        public async Task CreateAppointment_SlotAlreadyTaken_ThrowsAndCreatesNothing()
        {
            _repositoryMock
                .Setup(r => r.HasOverlapAsync(2, Start, End))
                .ReturnsAsync(true);

            var exception = await Assert.ThrowsAsync<SlotUnavailableException>(() => _sut.CreateAppointment(CreateValidDto()));

            StringAssert.Contains(exception.Message, "Dit tijdslot is niet meer beschikbaar");
            _repositoryMock.Verify(r => r.CreateWithReferralAsync(
                It.IsAny<CreateAppointmentDTO>(), It.IsAny<CreateReferralRequestDTO>()), Times.Never);
        }

        [TestMethod]
        public async Task CreateAppointment_RepositoryReportsRaceCondition_ThrowsSlotUnavailable()
        {
            // De pre-checks slagen allebei, maar een gelijktijdige boeking was net sneller
            _repositoryMock
                .Setup(r => r.CreateWithReferralAsync(It.IsAny<CreateAppointmentDTO>(), It.IsAny<CreateReferralRequestDTO>()))
                .ThrowsAsync(new SlotUnavailableException("Dit tijdslot is niet meer beschikbaar."));

            await Assert.ThrowsAsync<SlotUnavailableException>(() => _sut.CreateAppointment(CreateValidDto()));
        }

        [TestMethod]
        public async Task GetAppointmentAsync_ReturnsRepositoryResult()
        {
            var appointments = new List<Appointment> { new() { Id = 1 }, new() { Id = 2 } };
            _repositoryMock.Setup(r => r.GetAsync()).ReturnsAsync(appointments);

            var result = await _sut.GetAppointmentAsync();

            CollectionAssert.AreEqual(appointments, result);
        }
    }
}