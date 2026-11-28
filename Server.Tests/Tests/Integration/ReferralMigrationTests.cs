using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Server.Data;
using Server.Models.Intake;
using Server.Models.Enums;
using Server.Repositories;
using Server.Services;
using Server.Tests.Fixtures;
using Server.Tests.Utils;

namespace Server.Tests.Tests.Integration;

[TestClass]
[TestCategory("Integration")]
public class ReferralMigrationTests
{
    [TestMethod]
    public async Task PatientMigrationCanBeRolledBackAndReappliedWithoutLosingAReferral()
    {
        var options = NewTestDatabase();
        await using var database = new ReferralDbContext(options);
        var migrations = database.GetService<IMigrator>();

        try
        {
            await migrations.MigrateAsync();
            var service = new ReferralIntakeService(new ReferralIntakeRepository(database), new TreatmentCatalogRepository(database));
            var result = await service.CreateAsync(new ReferralRequests().Valid());
            Assert.IsNotNull(result.Referral);
            Assert.IsNotNull((await database.Referrals.SingleAsync()).PatientId);

            await migrations.MigrateAsync("20261126130000_MakeReferralPhoneOptional");
            await migrations.MigrateAsync();
            database.ChangeTracker.Clear();
            var restored = await database.Referrals.SingleAsync();
            Assert.AreEqual(result.Referral.Id, restored.Id);
            Assert.AreEqual("Z0005", restored.TreatmentCode);
            Assert.IsNull(restored.PatientId);

            await migrations.MigrateAsync(Migration.InitialDatabase);
            Assert.IsEmpty(await database.Database.GetAppliedMigrationsAsync());
        }
        finally
        {
            await database.Database.EnsureDeletedAsync();
        }
    }

    [TestMethod]
    public async Task ConcurrentRegistrationLinksReferralToThePatientAlreadyCommitted()
    {
        var options = NewTestDatabase();
        await using var database = new ReferralDbContext(options);
        try
        {
            await database.Database.MigrateAsync();
            var request = new ReferralRequests().Valid();
            var firstPatient = new Patient
            {
                Bsn = request.Bsn!, FullName = "Fictieve eerder geregistreerde patiënt",
                Email = request.PatientEmailAddress!, PostalCode = request.PatientPostalCode!,
                HouseNumber = request.PatientHouseNumber!
            };
            var racingOptions = new DbContextOptionsBuilder<ReferralDbContext>(options)
                .AddInterceptors(new RegisterPatientBeforeSave(options, firstPatient)).Options;
            await using var racing = new ReferralDbContext(racingOptions);
            var service = new ReferralIntakeService(new ReferralIntakeRepository(racing), new TreatmentCatalogRepository(racing));

            var created = await service.CreateAsync(request);

            Assert.IsNotNull(created.Referral);
            var patient = await database.Patients.SingleAsync();
            Assert.AreEqual(firstPatient.FullName, patient.FullName);
            var referral = await database.Referrals.SingleAsync();
            Assert.AreEqual(patient.Id, referral.PatientId);
            Assert.AreEqual(request.PatientName, referral.PatientName);
        }
        finally
        {
            await database.Database.EnsureDeletedAsync();
        }
    }

    [TestMethod]
    public async Task FailedPatientInsertCannotLeaveAnUnlinkedReferral()
    {
        await using var database = new ReferralDbContext(NewTestDatabase());
        try
        {
            await database.Database.MigrateAsync();
            var invalid = new Referral
            {
                Bsn = "000000001",
                PatientName = "Fictieve patiënt",
                PatientPostalCode = new string('X', 13),
                PatientHouseNumber = "12A",
                PatientEmailAddress = "patient@example.test",
                Specialty = "KNO",
                TreatmentCode = "Z0005",
                ReasonForReferral = "Fictieve reden",
                ClinicalQuestion = "Fictieve vraag",
                Medication = "Geen",
                MedicalInformation = "Fictieve bevinding",
                Allergies = "Geen",
                Urgency = ReferralUrgency.Regular
            };

            await Assert.ThrowsAsync<DbUpdateException>(() => new ReferralIntakeRepository(database)
                .AddAsync(invalid, CancellationToken.None));

            Assert.IsFalse(await database.Patients.AnyAsync());
            Assert.IsFalse(await database.Referrals.AnyAsync());
        }
        finally
        {
            await database.Database.EnsureDeletedAsync();
        }
    }

    private static DbContextOptions<ReferralDbContext> NewTestDatabase()
    {
        var connection = new SqlConnectionStringBuilder(TestSettings.ConnectionString);
        Assert.IsTrue(connection.InitialCatalog.EndsWith("Test", StringComparison.OrdinalIgnoreCase),
            "Use a dedicated SQL Server test connection ending in Test.");
        connection.InitialCatalog = $"ReferralIntake_{Guid.NewGuid():N}Test";
        return new DbContextOptionsBuilder<ReferralDbContext>().UseSqlServer(connection.ConnectionString).Options;
    }

    private class RegisterPatientBeforeSave(DbContextOptions<ReferralDbContext> options, Patient patient) : SaveChangesInterceptor
    {
        private bool _registered;

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (_registered) return result;

            _registered = true;
            await using var other = new ReferralDbContext(options);
            other.Patients.Add(patient);
            await other.SaveChangesAsync(cancellationToken);
            return result;
        }
    }
}
