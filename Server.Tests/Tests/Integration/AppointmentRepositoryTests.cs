using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Server.Models.DTO.Requests;
using Server.Models.Enums;
using Server.Repositories;
using Server.Repositories.InterFaces;
using Server.Tests.TestDb;

namespace Server.Tests.Integration
{
    [TestClass]
    public class AppointmentRepositoryTests : DatabaseTestBase
    {
        private IAppointmentRepository _sut = null!;

        [TestInitialize]
        public void CreateSut()
        {
            _sut = new AppointmentRepository(DbContext);
        }

        private static CreateAppointmentDTO CreateValidDto() => new()
        {
            PatientId = 1,
            SpecialistId = 1,
            ReferralId = 1,
            StartTime = new DateTime(2026, 12, 1, 9, 0, 0),
            EndTime = new DateTime(2026, 12, 1, 9, 30, 0)
        };

        private static async Task AssertForeignKeyViolationAsync(Func<Task> action, string constraintName)
        {
            var exception = await Assert.ThrowsAsync<DbUpdateException>(action);
            var sqlException = Assert.IsInstanceOfType<SqlException>(exception.InnerException);
            StringAssert.Contains(sqlException.Message, constraintName);
        }

        // ---------- CreateAsync ----------

        [TestMethod]
        public async Task CreateAsync_ValidDto_StoresAllFields()
        {
            // Arrange
            var dto = CreateValidDto();

            // Act
            var result = await _sut.CreateAsync(dto);

            // Assert
            Assert.IsTrue(result.Id > 0);

            var rows = await TestDatabase.QueryAsync(
                "SELECT * FROM Appointments WHERE Id = @id",
                new SqlParameter("@id", result.Id));

            Assert.AreEqual(1, rows.Rows.Count);
            var row = rows.Rows[0];
            Assert.AreEqual(dto.PatientId, row["PatientId"]);
            Assert.AreEqual(dto.SpecialistId, row["SpecialistId"]);
            Assert.AreEqual(dto.ReferralId, row["ReferralId"]);
            Assert.AreEqual(dto.StartTime, row["StartTime"]);
            Assert.AreEqual(dto.EndTime, row["EndTime"]);
        }

        [TestMethod]
        public async Task CreateAsync_ValidDto_SetsStatusToIngepland()
        {
            // Act
            var result = await _sut.CreateAsync(CreateValidDto());

            // Assert
            await using var verifyContext = TestDatabase.CreateDbContext();
            var stored = await verifyContext.Appointments.SingleAsync(a => a.Id == result.Id);
            Assert.AreEqual(AppointmentStatus.Ingepland, stored.Status);
        }

        [TestMethod]
        public async Task CreateAsync_UnknownPatient_ThrowsDbUpdateException()
        {
            var dto = CreateValidDto();
            dto.PatientId = 999;

            await AssertForeignKeyViolationAsync(
                () => _sut.CreateAsync(dto), "FK_Appointments_Patients_PatientId");
        }

        [TestMethod]
        public async Task CreateAsync_UnknownSpecialist_ThrowsDbUpdateException()
        {
            var dto = CreateValidDto();
            dto.SpecialistId = 999;

            await AssertForeignKeyViolationAsync(
                () => _sut.CreateAsync(dto), "FK_Appointments_Specialists_SpecialistId");
        }

        [TestMethod]
        public async Task CreateAsync_UnknownReferral_ThrowsDbUpdateException()
        {
            var dto = CreateValidDto();
            dto.ReferralId = 999;

            await AssertForeignKeyViolationAsync(
                () => _sut.CreateAsync(dto), "FK_Appointments_Referrals_ReferralId");
        }

        [TestMethod]
        public async Task CreateAsync_SecondAppointmentForSameReferral_ThrowsDbUpdateException()
        {
            // Arrange: referral 1 heeft al een afspraak
            await _sut.CreateAsync(CreateValidDto());

            var second = CreateValidDto();
            second.StartTime = new DateTime(2026, 12, 2, 9, 0, 0);
            second.EndTime = new DateTime(2026, 12, 2, 9, 30, 0);

            // Act + Assert (EF maakt bij een 1-op-1 relatie een unique index)
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => _sut.CreateAsync(second));
            Assert.IsInstanceOfType<SqlException>(exception.InnerException);
        }

        // ---------- GetAsync ----------

        [TestMethod]
        public async Task GetAsync_NoAppointments_ReturnsEmptyList()
        {
            // Act
            var result = await _sut.GetAsync();

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public async Task GetAsync_TwoAppointments_ReturnsBoth()
        {
            // Arrange
            var first = CreateValidDto();
            var second = CreateValidDto();
            second.ReferralId = 2; // referral 1 is al in gebruik
            second.StartTime = new DateTime(2026, 12, 2, 9, 0, 0);
            second.EndTime = new DateTime(2026, 12, 2, 9, 30, 0);

            var created1 = await _sut.CreateAsync(first);
            var created2 = await _sut.CreateAsync(second);

            // Act
            var result = await _sut.GetAsync();

            // Assert
            Assert.AreEqual(2, result.Count);
            CollectionAssert.AreEquivalent(
                new[] { created1.Id, created2.Id },
                result.Select(a => a.Id).ToArray());
        }
    }
}