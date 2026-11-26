using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Server.Models.DTO.Requests;
using Server.Models.Enums;
using Server.Repositories;
using Server.Repositories.InterFaces;
using Server.Tests.TestDb;

namespace Server.Tests.Tests.Integration
{
    [TestClass]
    public class ReferralRepositoryTests : DatabaseTestBase
    {
        private ReferralRepository _sut = null!;

        protected override Func<DbContext, Task>? GetTestDataInitializer()
        {
            return TestAFSDataInitializer.SeedAsync;
        }

        [TestInitialize]
        public async Task CreateSut()
        {
            _sut = new ReferralRepository(DbContext);
        }

        private static CreateReferralRequestDTO CreateValidDto() => new()
        {
            PatientId = 1,
            GeneralPractitionerId = 1,
            Specialism = Specialism.Cardiologie,
            Reason = "Pijn op de borst",
            Urgency = "Normaal",
            TreatmentCode = "ABC123",
            Questionnaire = "Vragenlijst"
        };

        [TestMethod]
        public async Task CreateAsync_ValidDto_StoresAllFields()
        {
            // Arrange
            var dto = CreateValidDto();

            // Act
            var result = await _sut.CreateAsync(dto);

            // Assert
            Assert.IsGreaterThan(0, result.Id);

            var rows = await TestDatabase.QueryAsync(
                "SELECT * FROM Referrals WHERE Id = @id",
                new SqlParameter("@id", result.Id));

            Assert.HasCount(1, rows.Rows);
            var row = rows.Rows[0];
            Assert.AreEqual(dto.PatientId, row["PatientId"]);
            Assert.AreEqual(dto.GeneralPractitionerId, row["GeneralPractitionerId"]);
            Assert.AreEqual(dto.Reason, row["Reason"]);
            Assert.AreEqual(dto.Questionnaire, row["Questionnaire"]);
            Assert.AreEqual(dto.TreatmentCode, row["TreatmentCode"]);
        }

        [TestMethod]
        public async Task CreateAsync_ValidDto_SetsStatusToOpen()
        {
            // Act
            var result = await _sut.CreateAsync(CreateValidDto());

            // Assert
            var rows = await TestDatabase.QueryAsync(
                "SELECT Status FROM Referrals WHERE Id = @id",
                new SqlParameter("@id", result.Id));

            Assert.AreEqual("Open", rows.Rows[0]["Status"]);
        }

        [TestMethod]
        public async Task CreateAsync_NullQuestionnaire_StoresEmptyString()
        {
            // Arrange
            var dto = CreateValidDto();
            dto.Questionnaire = null;

            // Act
            var result = await _sut.CreateAsync(dto);

            // Assert
            var rows = await TestDatabase.QueryAsync(
                "SELECT Questionnaire FROM Referrals WHERE Id = @id",
                new SqlParameter("@id", result.Id));

            Assert.AreEqual(string.Empty, rows.Rows[0]["Questionnaire"]);
        }

        [TestMethod]
        [DataRow(Specialism.Cardiologie)]
        [DataRow(Specialism.KNO)]
        [DataRow(Specialism.Algemeen)]
        public async Task CreateAsync_Specialism_IsStoredCorrectly(Specialism specialism)
        {
            // Arrange
            var dto = CreateValidDto();
            dto.Specialism = specialism;

            // Act
            var result = await _sut.CreateAsync(dto);

            // Assert
            await using var verifyContext = TestDatabase.CreateDbContext();
            var stored = await verifyContext.Referrals.SingleAsync(r => r.Id == result.Id, TestContext.CancellationToken);
            Assert.AreEqual(specialism, stored.Specialism);
        }

        [TestMethod]
        public async Task CreateAsync_ValidDto_SetsCreatedAtToUtcNow()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var result = await _sut.CreateAsync(CreateValidDto());

            // Assert
            var after = DateTime.UtcNow;
            Assert.IsTrue(result.CreatedAt >= before && result.CreatedAt <= after);

            var rows = await TestDatabase.QueryAsync(
                "SELECT CreatedAt FROM Referrals WHERE Id = @id",
                new SqlParameter("@id", result.Id));
            var stored = (DateTime)rows.Rows[0]["CreatedAt"];
            Assert.IsLessThan(TimeSpan.FromSeconds(5), (stored - DateTime.UtcNow).Duration());
        }

        [TestMethod]
        public async Task CreateAsync_UnknownPatient_ThrowsDbUpdateException()
        {
            // Arrange
            var dto = CreateValidDto();
            dto.PatientId = 999;

            // Act + Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _sut.CreateAsync(dto));
        }

        [TestMethod]
        public async Task CreateAsync_UnknownGeneralPractitioner_ThrowsDbUpdateException()
        {
            // Arrange
            var dto = CreateValidDto();
            dto.GeneralPractitionerId = 999;

            // Act + Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _sut.CreateAsync(dto));
        }
        [TestMethod]
        public async Task CreateAsync_WithPreferredSpecialist_StoresPreferredSpecialistId()
        {
            // Arrange
            var dto = CreateValidDto();
            dto.PreferredSpecialistId = 1;

            // Act
            var result = await _sut.CreateAsync(dto);

            // Assert
            var rows = await TestDatabase.QueryAsync(
                "SELECT PreferredSpecialistId FROM Referrals WHERE Id = @id",
                new SqlParameter("@id", result.Id));
            Assert.AreEqual(1, rows.Rows[0]["PreferredSpecialistId"]);
        }

        [TestMethod]
        public async Task CreateAsync_UnknownPreferredSpecialist_ThrowsDbUpdateException()
        {
            // Arrange
            var dto = CreateValidDto();
            dto.PreferredSpecialistId = 999;

            // Act
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _sut.CreateAsync(dto));

            // Assert
            var sqlException = Assert.IsInstanceOfType<SqlException>(exception.InnerException);
            Assert.Contains("FK_Referrals_Specialists_PreferredSpecialistId", sqlException.Message);
        }

        [TestMethod]
        public async Task CreateAsync_UnknownTreatmentCode_ThrowsDbUpdateException()
        {
            // Arrange
            var dto = CreateValidDto();
            dto.TreatmentCode = "BESTAATNIET";

            // Act
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _sut.CreateAsync(dto));

            // Assert
            var sqlException = Assert.IsInstanceOfType<SqlException>(exception.InnerException);
            Assert.Contains("FK_Referrals_TreatmentCodes_TreatmentCode", sqlException.Message);
        }

    }
}