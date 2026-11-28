using Microsoft.EntityFrameworkCore;
using Server.Models;
using Server.Models.Enums;
using Server.Repositories;
using Server.Repositories.InterFaces;
using Server.Tests.TestDb;

namespace Server.Tests.Tests.Integration
{
    [TestClass]
    public class PlanningRepositoryTests : DatabaseTestBase
    {
        private PlanningRepository _sut = null!;

        protected override Func<DbContext, Task>? GetTestDataInitializer()
        {
            return TestAFSDataInitializer.SeedAsync;
        }

        [TestInitialize]
        public Task CreateSut()
        {
            _sut = new PlanningRepository(DbContext);
            return Task.CompletedTask;
        }

        public async Task CreateAppointmentAsync(Specialist specialist)
        {
            var patient = await DbContext.Patients.FirstAsync();

            var appointment = new Appointment
            {
                PatientId = patient.Id,
                SpecialistId = specialist.Id,
                StartTime = new DateTime(2027, 1, 5, 10, 0, 0),
                EndTime = new DateTime(2027, 1, 5, 11, 0, 0),
                Status = AppointmentStatus.Ingepland
            };

            DbContext.Appointments.Add(appointment);
            await DbContext.SaveChangesAsync();
        }

        [TestMethod]
        public async Task GetAllPatientsAsync_ReturnsPatientsWithFullName()
        {
            // Act
            var result = await _sut.GetAllPatientsAsync();

            // Assert
            Assert.IsNotEmpty(result);

            foreach (var patient in result)
            {
                Assert.IsTrue(patient.patientId > 0);
                Assert.IsFalse(string.IsNullOrWhiteSpace(patient.patientFullName));
            }
        }

        [TestMethod]
        public async Task GetCodesBySpecialism_ValidSpecialism_ReturnsMatchingCodes()
        {
            // Arrange
            var existingCode = await DbContext.TreatmentCodes.FirstAsync(TestContext.CancellationToken);
            var specialism = existingCode.Specialism.ToString();

            // Act
            var result = await _sut.GetCodesBySpecialism(specialism);

            // Assert
            Assert.IsNotEmpty(result);
            Assert.IsTrue(result.All(c => c.Specialism == existingCode.Specialism));
        }

        [TestMethod]
        public async Task GetCodesBySpecialism_InvalidSpecialism_ReturnsEmptyList()
        {
            // Act
            var result = await _sut.GetCodesBySpecialism("BestaatNiet");

            // Assert
            Assert.IsEmpty(result);
        }

        [TestMethod]
        public async Task GetSpecialistsBySpecialism_ValidSpecialism_ReturnsMatchingSpecialists()
        {
            // Arrange
            var existingSpecialist = await DbContext.Specialists.FirstAsync(TestContext.CancellationToken);
            var specialism = existingSpecialist.Specialism.ToString();

            // Act
            var result = await _sut.GetSpecialistsBySpecialism(specialism);

            // Assert
            Assert.IsNotEmpty(result);

            foreach (var specialist in result)
            {
                Assert.IsTrue(specialist.SpecialistId > 0);
                Assert.IsFalse(string.IsNullOrWhiteSpace(specialist.SpecialistFullName));
                Assert.AreEqual(specialism, specialist.Specialism);
            }
        }

        [TestMethod]
        public async Task HasOverlapAsync_ExistingAppointment_ReturnsTrue()
        {
            // Arrange
            var specialist = await DbContext.Specialists.FirstAsync(TestContext.CancellationToken);
            await CreateAppointmentAsync(specialist);

            var appointment = await DbContext.Appointments.FirstAsync(TestContext.CancellationToken);

            // Act
            var result = await _sut.HasOverlapAsync(
                appointment.SpecialistId,
                appointment.StartTime,
                appointment.EndTime);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task GetBusyIntervalsAsync_WithAppointmentAndExternalEvent_ReturnsBothIntervals()
        {
            // Arrange
            var specialist = await DbContext.Specialists.FirstAsync();
            await CreateAppointmentAsync(specialist);

            DbContext.ExternalCalendarEvents.Add(new ExternalCalendarEvent
            {
                SpecialistId = specialist.Id,
                ExternalUid = "ext-123",
                Title = "Privé afspraak",
                StartUtc = new DateTime(2027, 1, 5, 13, 0, 0, DateTimeKind.Utc), // 14:00 CET
                EndUtc = new DateTime(2027, 1, 5, 14, 0, 0, DateTimeKind.Utc),   // 15:00 CET
                IsAllDay = false
            });
            await DbContext.SaveChangesAsync();

            var windowStart = new DateTime(2027, 1, 5, 0, 0, 0);
            var windowEnd = new DateTime(2027, 1, 26, 0, 0, 0);

            // Act
            var result = await _sut.GetBusyIntervalsAsync(specialist.Id, windowStart, windowEnd);

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(r =>
                r.Start == new DateTime(2027, 1, 5, 10, 0, 0) &&
                r.End == new DateTime(2027, 1, 5, 11, 0, 0)));
            Assert.IsTrue(result.Any(r =>
                r.Start == new DateTime(2027, 1, 5, 14, 0, 0) &&
                r.End == new DateTime(2027, 1, 5, 15, 0, 0)));
        }

        [TestMethod]
        public async Task GetBusyIntervalsAsync_OutsideSearchWindow_ReturnsEmptyList()
        {
            // Arrange
            var specialist = await DbContext.Specialists.FirstAsync();
            await CreateAppointmentAsync(specialist); // Gepland op 5 jan 2027

            var windowStart = new DateTime(2027, 2, 1, 0, 0, 0);
            var windowEnd = new DateTime(2027, 2, 22, 0, 0, 0);

            // Act
            var result = await _sut.GetBusyIntervalsAsync(specialist.Id, windowStart, windowEnd);

            // Assert
            Assert.AreEqual(0, result.Count);
        }
    }
}
