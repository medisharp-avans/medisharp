using Moq;
using Server.Models;
using Server.Repositories.InterFaces;
using Server.Services;

namespace Server.Tests.Unit
{
    [TestClass]
    public class CalculateServiceTests
    {
        private Mock<ITreatmentCodeRepository> _repositoryMock = null!;
        private CalculateService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<ITreatmentCodeRepository>();
            _sut = new CalculateService(_repositoryMock.Object);
        }

        [TestMethod]
        [DataRow(1, 15)]
        [DataRow(2, 30)]
        [DataRow(4, 60)]
        [DataRow(6, 90)]
        public async Task CalculateAppointmentEndTime_ValidCode_AddsQuartersAsMinutes(
            int quarters, int expectedMinutes)
        {
            // Arrange
            var startTime = new DateTime(2026, 11, 21, 9, 0, 0);

            _repositoryMock
                .Setup(r => r.GetByCodeAsync("ABC123"))
                .ReturnsAsync(new TreatmentCode
                {
                    Code = "ABC123",
                    DurationInQuarters = quarters
                });

            // Act
            var result = await _sut.CalculateAppointmentEndTime(startTime, "ABC123");

            // Assert
            Assert.AreEqual(startTime.AddMinutes(expectedMinutes), result);
        }

        [TestMethod]
        public async Task CalculateAppointmentEndTime_ZeroQuarters_ReturnsStartTime()
        {
            // Arrange
            var startTime = new DateTime(2026, 11, 21, 9, 0, 0);

            _repositoryMock
                .Setup(r => r.GetByCodeAsync("ZERO"))
                .ReturnsAsync(new TreatmentCode { Code = "ZERO", DurationInQuarters = 0 });

            // Act
            var result = await _sut.CalculateAppointmentEndTime(startTime, "ZERO");

            // Assert
            Assert.AreEqual(startTime, result);
        }

        [TestMethod]
        public async Task CalculateAppointmentEndTime_CrossesMidnight_ReturnsNextDay()
        {
            // Arrange
            var startTime = new DateTime(2026, 11, 21, 23, 30, 0);

            _repositoryMock
                .Setup(r => r.GetByCodeAsync("LATE"))
                .ReturnsAsync(new TreatmentCode { Code = "LATE", DurationInQuarters = 4 });

            // Act
            var result = await _sut.CalculateAppointmentEndTime(startTime, "LATE");

            // Assert
            Assert.AreEqual(new DateTime(2026, 11, 22, 0, 30, 0), result);
        }

        [TestMethod]
        public async Task CalculateAppointmentEndTime_UnknownCode_ThrowsKeyNotFoundException()
        {
            // Arrange
            var startTime = new DateTime(2026, 11, 21, 9, 0, 0);

            _repositoryMock
                .Setup(r => r.GetByCodeAsync("ONBEKEND"))
                .ReturnsAsync((TreatmentCode?)null);

            // Act
            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sut.CalculateAppointmentEndTime(startTime, "ONBEKEND"));

            // Assert
            Assert.AreEqual("Zorgcode ONBEKEND bestaat niet.", exception.Message);
        }

        [TestMethod]
        public async Task CalculateAppointmentEndTime_ValidCode_CallsRepositoryExactlyOnce()
        {
            // Arrange
            var startTime = new DateTime(2026, 11, 21, 9, 0, 0);

            _repositoryMock
                .Setup(r => r.GetByCodeAsync("ABC123"))
                .ReturnsAsync(new TreatmentCode { Code = "ABC123", DurationInQuarters = 2 });

            // Act
            await _sut.CalculateAppointmentEndTime(startTime, "ABC123");

            // Assert
            _repositoryMock.Verify(r => r.GetByCodeAsync("ABC123"), Times.Once);
        }
    }
}