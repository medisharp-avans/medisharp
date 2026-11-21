using Moq;
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
        private Mock<IReferralService> _referralServiceMock = null!;
        private Mock<ICalculateService> _calculateServiceMock = null!;
        private AppointmentService _sut = null!;

        private static readonly DateTime Start = new(2027, 1, 12, 10, 0, 0);
        private static readonly DateTime End = new(2027, 1, 12, 11, 0, 0);

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<IAppointmentRepository>();
            _referralServiceMock = new Mock<IReferralService>();
            _calculateServiceMock = new Mock<ICalculateService>();
            _sut = new AppointmentService(
                _repositoryMock.Object,
                _referralServiceMock.Object,
                _calculateServiceMock.Object);

            // Standaard succespad; individuele tests overschrijven wat nodig is
            _calculateServiceMock
                .Setup(c => c.CalculateAppointmentEndTime(Start, "KNO002"))
                .ReturnsAsync(End);
            _referralServiceMock
                .Setup(r => r.CreateAsync(It.IsAny<CreateReferralRequestDTO>()))
                .ReturnsAsync(new Referral { Id = 42 });
            _repositoryMock
                .Setup(r => r.CreateAsync(It.IsAny<CreateAppointmentDTO>()))
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
            // Act
            var result = await _sut.CreateAppointment(CreateValidDto());

            // Assert
            Assert.AreEqual(7, result.Id);
        }

        [TestMethod]
        public async Task CreateAppointment_ValidDto_PassesCalculatedEndTimeToRepository()
        {
            // Act
            await _sut.CreateAppointment(CreateValidDto());

            // Assert
            _repositoryMock.Verify(r => r.CreateAsync(It.Is<CreateAppointmentDTO>(d =>
                d.StartTime == Start && d.EndTime == End)), Times.Once);
        }

        [TestMethod]
        public async Task CreateAppointment_ValidDto_MapsAppointmentFields()
        {
            // Act
            await _sut.CreateAppointment(CreateValidDto());

            // Assert
            _repositoryMock.Verify(r => r.CreateAsync(It.Is<CreateAppointmentDTO>(d =>
                d.PatientId == 1 && d.SpecialistId == 2)), Times.Once);
        }

        [TestMethod]
        public async Task CreateAppointment_ValidDto_CreatesReferralWithMappedFields()
        {
            // Arrange
            CreateReferralRequestDTO? captured = null;
            _referralServiceMock
                .Setup(r => r.CreateAsync(It.IsAny<CreateReferralRequestDTO>()))
                .Callback<CreateReferralRequestDTO>(d => captured = d)
                .ReturnsAsync(new Referral { Id = 42 });

            // Act
            await _sut.CreateAppointment(CreateValidDto());

            // Assert
            Assert.IsNotNull(captured);
            Assert.AreEqual(1, captured.PatientId);
            Assert.AreEqual(3, captured.GeneralPractitionerId);
            Assert.AreEqual(Specialism.KNO, captured.Specialism);
            Assert.AreEqual(2, captured.PreferredSpecialistId);
            Assert.AreEqual("Oorpijn", captured.Reason);
            Assert.AreEqual("Vragenlijst", captured.Questionnaire);
            Assert.AreEqual("Regulier", captured.Urgency);
            Assert.AreEqual("KNO002", captured.TreatmentCode);
        }

        [TestMethod]
        public async Task CreateAppointment_ValidDto_LinksAppointmentToCreatedReferral()
        {
            // Act
            await _sut.CreateAppointment(CreateValidDto());

            // Assert
            _repositoryMock.Verify(r => r.CreateAsync(It.Is<CreateAppointmentDTO>(d =>
                d.ReferralId == 42)), Times.Once);
        }

        [TestMethod]
        public async Task CreateAppointment_UnknownTreatmentCode_ThrowsAndCreatesNothing()
        {
            // Arrange
            _calculateServiceMock
                .Setup(c => c.CalculateAppointmentEndTime(It.IsAny<DateTime>(), "ONBEKEND"))
                .ThrowsAsync(new KeyNotFoundException("Zorgcode ONBEKEND bestaat niet."));
            var dto = CreateValidDto();
            dto.TreatmentCode = "ONBEKEND";

            // Act
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.CreateAppointment(dto));

            // Assert: geen wees-verwijzing en geen afspraak
            _referralServiceMock.Verify(
                r => r.CreateAsync(It.IsAny<CreateReferralRequestDTO>()), Times.Never);
            _repositoryMock.Verify(
                r => r.CreateAsync(It.IsAny<CreateAppointmentDTO>()), Times.Never);
        }

        [TestMethod]
        public async Task CreateAppointment_ReferralCreationFails_DoesNotCreateAppointment()
        {
            // Arrange
            _referralServiceMock
                .Setup(r => r.CreateAsync(It.IsAny<CreateReferralRequestDTO>()))
                .ThrowsAsync(new InvalidOperationException("Verwijzing mislukt"));

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.CreateAppointment(CreateValidDto()));

            // Assert
            _repositoryMock.Verify(
                r => r.CreateAsync(It.IsAny<CreateAppointmentDTO>()), Times.Never);
        }

        [TestMethod]
        public async Task GetAppointmentAsync_ReturnsRepositoryResult()
        {
            // Arrange
            var appointments = new List<Appointment> { new() { Id = 1 }, new() { Id = 2 } };
            _repositoryMock.Setup(r => r.GetAsync()).ReturnsAsync(appointments);

            // Act
            var result = await _sut.GetAppointmentAsync();

            // Assert
            CollectionAssert.AreEqual(appointments, result);
        }
    }
}