using Referral = Server.Models.Intake.Referral;
using Server.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Server.Data;
using Server.Models;

namespace Server.Tests
{
    [TestClass]
    public class ReferralModelTests
    {
        [TestMethod]
        public void SqlServerSchemaSupportsRequiredReferralDataAndOptionalAttachments()
        {
            using var context = CreateContext();

            // Generate SQL with the real provider, without connecting to a database.
            var script = context.Database.GenerateCreateScript();

            StringAssert.Contains(script, "CREATE TABLE [Referrals]");
            StringAssert.Contains(script, "[Bsn] varchar(9) NOT NULL");
            StringAssert.Contains(script, "[Specialty] nvarchar(100) NOT NULL");
            StringAssert.Contains(script, "[TreatmentCode] nvarchar(50) NOT NULL");
            StringAssert.Contains(script, "[PatientPostalCode] nvarchar(12) NULL");
            StringAssert.Contains(script, "[PatientHouseNumber] nvarchar(20) NULL");
            StringAssert.Contains(script, "[PatientPhoneNumber] nvarchar(32) NULL");
            StringAssert.Contains(script, "[Status] nvarchar(32) NOT NULL DEFAULT N'Open'");
            StringAssert.Contains(script, "CREATE TABLE [ReferralAttachments]");
            StringAssert.Contains(script, "FOREIGN KEY ([ReferralId]) REFERENCES [Referrals] ([Id])");

            Assert.HasCount(4, context.Model.GetEntityTypes().ToArray());
        }

        [TestMethod]
        public void PatientMigrationAddsUniqueBsnAndKeepsExistingReferrals()
        {
            using var context = CreateContext();

            Assert.IsFalse(context.Database.HasPendingModelChanges());
            var script = context.GetService<IMigrator>()
                .GenerateScript("20261127170000_AddReferralPostalCodeAndHouseNumber");

            StringAssert.Contains(script, "CREATE TABLE [Patients]");
            StringAssert.Contains(script, "CREATE UNIQUE INDEX [IX_Patients_Bsn] ON [Patients] ([Bsn])");
            StringAssert.Contains(script, "ALTER TABLE [Referrals] ADD [PatientId] int NULL");
            StringAssert.Contains(script, "FOREIGN KEY ([PatientId]) REFERENCES [Patients] ([Id])");
            Assert.IsFalse(script.Contains("DROP", StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void PriAndReferralMigrationHistoriesDoNotOverlap()
        {
            using var intake = CreateContext();
            using var accounts = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=localhost;Database=PriMigrationTests;Integrated Security=True;TrustServerCertificate=True")
                .Options);

            Assert.IsFalse(accounts.Database.HasPendingModelChanges());
            Assert.IsFalse(accounts.Database.GetMigrations().Intersect(intake.Database.GetMigrations()).Any());
            var script = accounts.GetService<IMigrator>().GenerateScript();
            StringAssert.Contains(script, "CREATE TABLE [Users]");
            StringAssert.Contains(script, "CREATE TABLE [RefreshTokens]");
        }

        [TestMethod]
        public void TwoReferralsForSamePatientKeepSeparateSnapshotsAndAttachmentOwnership()
        {
            using var context = CreateContext();
            var firstReferral = CreateReferral("1234 AB");
            var secondReferral = CreateReferral("5678 CD");
            var attachment = new ReferralAttachment
            {
                FileName = "uitslag.pdf",
                ContentType = "application/pdf",
                StorageKey = "test/uitslag",
                SizeInBytes = 100
            };
            secondReferral.Attachments.Add(attachment);

            context.Referrals.AddRange(firstReferral, secondReferral);

            Assert.AreNotEqual(firstReferral.Id, secondReferral.Id);
            Assert.AreEqual("000000000", firstReferral.Bsn);
            Assert.AreEqual(firstReferral.Bsn, secondReferral.Bsn);
            Assert.AreEqual("1234 AB", firstReferral.PatientPostalCode);
            Assert.AreEqual("5678 CD", secondReferral.PatientPostalCode);
            Assert.AreEqual(ReferralStatus.Open, firstReferral.Status);
            Assert.AreEqual(ReferralStatus.Open, secondReferral.Status);
            Assert.IsEmpty(firstReferral.Attachments);
            Assert.AreEqual(secondReferral.Id, attachment.ReferralId);
            Assert.AreSame(secondReferral, attachment.Referral);
            Assert.HasCount(2, context.ChangeTracker.Entries<Referral>().ToArray());
            Assert.HasCount(1, context.ChangeTracker.Entries<ReferralAttachment>().ToArray());
        }

        private static ReferralDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ReferralDbContext>()
                .UseSqlServer("Server=localhost;Database=ReferralModelTests;Integrated Security=True;TrustServerCertificate=True")
                .Options;

            return new ReferralDbContext(options);
        }

        private static Referral CreateReferral(string postalCode) => new()
        {
            Bsn = "000000000", // Synthetic value; BSN validation belongs to the later API phase.
            PatientName = "Testpatient",
            PatientPostalCode = postalCode,
            PatientHouseNumber = "12A",
            PatientPhoneNumber = "0000000000",
            PatientEmailAddress = "patient@example.test",
            Specialty = "KNO",
            TreatmentCode = "Z0005",
            ReasonForReferral = "Oorpijn sinds twee weken",
            ClinicalQuestion = "Wat is de oorzaak van de oorpijn?",
            Medication = "Geen",
            MedicalInformation = "Alleen relevante bevindingen voor deze verwijzing",
            Allergies = "Geen bekend",
            Urgency = ReferralUrgency.Regular
        };
    }
}
