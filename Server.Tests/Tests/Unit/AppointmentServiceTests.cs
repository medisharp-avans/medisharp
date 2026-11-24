using Moq;
using Server.Exceptions;
using Server.Models;
using Server.Models.DTO;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Repositories.InterFaces;
using Server.Services;
using Server.Services.InterFaces;

namespace Server.Tests.Unit
{
    /// <summary>
    /// AppointmentService kiest zelf geen specialist of tijdstip meer: dat doet IPlanningService.
    /// De beschikbaarheids- en overlapcontroles worden getest in PlanningServiceTests.
    /// </summary>
    [TestClass]
    public class AppointmentServiceTests
    {
        private Mock<IAppointmentRepository> _repositoryMock = null!;
        private Mock<IPlanningService> _planningServiceMock = null!;
        private Mock<IPlanningRepository> _planningRepositoryMock = null!;
        private AppointmentService _sut = null!;

        private static readonly DateTime Start = new(2027, 1, 12, 10, 0, 0);
        private static readonly DateTime End = new(2027, 1, 12, 11, 0, 0);

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<IAppointmentRepository>();
            _planningServiceMock = new Mock<IPlanningService>();
            _planningRepositoryMock = new Mock<IPlanningRepository>();

            _sut = new AppointmentService(
                _repositoryMock.Object,
                _planningServiceMock.Object,
                _planningRepositoryMock.Object);

            // Standaard succespad; individuele tests overschrijven wat nodig is
            _planningServiceMock
                .Setup(p => p.SetSpecialistForAppointment(It.IsAny<AppointmentManualRequestDTO>()))
                .ReturnsAsync(new SpecialistAppointmentDTO { SpecialistId = 2, StartTime = Start, EndTime = End });
            _planningRepositoryMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(new Patient { Id = 1, GeneralPractitionerId = 3 });
            _repositoryMock
                .Setup(r => r.CreateWithReferralAsync(It.IsAny<CreateAppointmentDTO>(), It.IsAny<CreateReferralRequestDTO>()))
                .ReturnsAsync(new Appointment { Id = 7 });
        }

        private static AppointmentManualRequestDTO CreateValidDto() => new()
        {
            PatientId = 1,
            Specialism = Specialism.KNO,
            PreferredSpecialistId = 2,
            Reason = "Oorpijn",
            Questionnaire = "Vragenlijst",
            Urgency = "Regulier",
            TreatmentCode = "KNO002"
        };

        private void VerifyNothingCreated() =>
            _repositoryMock.Verify(r => r.CreateWithReferralAsync(
                It.IsAny<CreateAppointmentDTO>(), It.IsAny<CreateReferralRequestDTO>()), Times.Never);

        [TestMethod]
        public async Task CreateAppointment_ValidDto_ReturnsAppointmentFromRepository()
        {
            var result = await _sut.CreateAppointment(CreateValidDto());

            Assert.AreEqual(7, result.Id);
        }

        [TestMethod]
        public async Task CreateAppointment_ValidDto_AsksPlanningServiceForSpecialistAndTime()
        {
            var dto = CreateValidDto();

            await _sut.CreateAppointment(dto);

            _planningServiceMock.Verify(p => p.SetSpecialistForAppointment(dto), Times.Once);
        }

        [TestMethod]
        public async Task CreateAppointment_ValidDto_UsesSpecialistAndTimesChosenByPlanning()
        {
            // De planning kiest een andere specialist dan de voorkeur (2) en andere tijden
            var chosenStart = new DateTime(2027, 1, 13, 14, 0, 0);
            var chosenEnd = new DateTime(2027, 1, 13, 14, 30, 0);

            _planningServiceMock
                .Setup(p => p.SetSpecialistForAppointment(It.IsAny<AppointmentManualRequestDTO>()))
                .ReturnsAsync(new SpecialistAppointmentDTO { SpecialistId = 5, StartTime = chosenStart, EndTime = chosenEnd });

            await _sut.CreateAppointment(CreateValidDto());

            _repositoryMock.Verify(r => r.CreateWithReferralAsync(
                It.Is<CreateAppointmentDTO>(a =>
                    a.PatientId == 1 && a.SpecialistId == 5 && a.StartTime == chosenStart && a.EndTime == chosenEnd),
                It.IsAny<CreateReferralRequestDTO>()), Times.Once);
        }

        [TestMethod]
        public async Task CreateAppointment_ValidDto_CreatesReferralWithMappedFields()
        {
            await _sut.CreateAppointment(CreateValidDto());

            // De huisarts komt van de patiënt, de voorkeursspecialist van het verzoek
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
        public async Task CreateAppointment_NoPreferredSpecialist_ReferralKeepsNoPreference()
        {
            // De planning wijst zelf een specialist toe; dat is geen voorkeur van de patiënt
            var dto = CreateValidDto();
            dto.PreferredSpecialistId = null;

            await _sut.CreateAppointment(dto);

            _repositoryMock.Verify(r => r.CreateWithReferralAsync(
                It.Is<CreateAppointmentDTO>(a => a.SpecialistId == 2),
                It.Is<CreateReferralRequestDTO>(d => d.PreferredSpecialistId == null)), Times.Once);
        }

        [TestMethod]
        public async Task CreateAppointment_UnknownPatient_ThrowsAndCreatesNothing()
        {
            _planningRepositoryMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync((Patient?)null);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.CreateAppointment(CreateValidDto()));

            StringAssert.Contains(exception.Message, "patiënt");
            VerifyNothingCreated();
        }

        [TestMethod]
        public async Task CreateAppointment_PlanningFindsNoSpecialist_ThrowsAndCreatesNothing()
        {
            _planningServiceMock
                .Setup(p => p.SetSpecialistForAppointment(It.IsAny<AppointmentManualRequestDTO>()))
                .ThrowsAsync(new SlotUnavailableException("Geen beschikbare specialist gevonden."));

            await Assert.ThrowsAsync<SlotUnavailableException>(() => _sut.CreateAppointment(CreateValidDto()));

            VerifyNothingCreated();
        }

        [TestMethod]
        public async Task CreateAppointment_UnknownTreatmentCode_ThrowsAndCreatesNothing()
        {
            _planningServiceMock
                .Setup(p => p.SetSpecialistForAppointment(It.IsAny<AppointmentManualRequestDTO>()))
                .ThrowsAsync(new KeyNotFoundException("Zorgcode ONBEKEND bestaat niet."));
            var dto = CreateValidDto();
            dto.TreatmentCode = "ONBEKEND";

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.CreateAppointment(dto));

            VerifyNothingCreated();
        }

        [TestMethod]
        public async Task CreateAppointment_RepositoryReportsRaceCondition_ThrowsSlotUnavailable()
        {
            // De planning vond een vrije specialist, maar een gelijktijdige boeking was net sneller
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