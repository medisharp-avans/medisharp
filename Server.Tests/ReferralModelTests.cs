using Microsoft.EntityFrameworkCore;
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
            StringAssert.Contains(script, "[Status] nvarchar(32) NOT NULL DEFAULT N'Open'");
            StringAssert.Contains(script, "CREATE TABLE [ReferralAttachments]");
            StringAssert.Contains(script, "FOREIGN KEY ([ReferralId]) REFERENCES [Referrals] ([Id])");

            Assert.HasCount(2, context.Model.GetEntityTypes().ToArray());
        }

        [TestMethod]
        public void TwoReferralsForSamePatientKeepSeparateSnapshotsAndAttachmentOwnership()
        {
            using var context = CreateContext();
            var firstReferral = CreateReferral("Oud adres 1");
            var secondReferral = CreateReferral("Nieuw adres 2");
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
            Assert.AreEqual("Oud adres 1", firstReferral.PatientAddress);
            Assert.AreEqual("Nieuw adres 2", secondReferral.PatientAddress);
            Assert.AreEqual(ReferralStatus.Open, firstReferral.Status);
            Assert.AreEqual(ReferralStatus.Open, secondReferral.Status);
            Assert.IsEmpty(firstReferral.Attachments);
            Assert.AreEqual(secondReferral.Id, attachment.ReferralId);
            Assert.AreSame(secondReferral, attachment.Referral);
            Assert.HasCount(2, context.ChangeTracker.Entries<Referral>().ToArray());
            Assert.HasCount(1, context.ChangeTracker.Entries<ReferralAttachment>().ToArray());
        }

        private static AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=localhost;Database=ReferralModelTests;Integrated Security=True;TrustServerCertificate=True")
                .Options;

            return new AppDbContext(options);
        }

        private static Referral CreateReferral(string address) => new()
        {
            Bsn = "000000000", // Synthetic value; BSN validation belongs to the later API phase.
            PatientName = "Testpatient",
            PatientAddress = address,
            PatientCity = "Teststad",
            PatientPhoneNumber = "0000000000",
            PatientEmailAddress = "patient@example.test",
            Specialty = "KNO",
            TreatmentCode = "TEST-KNO",
            ReasonForReferral = "Oorpijn sinds twee weken",
            ClinicalQuestion = "Wat is de oorzaak van de oorpijn?",
            Medication = "Geen",
            MedicalInformation = "Alleen relevante bevindingen voor deze verwijzing",
            Allergies = "Geen bekend",
            Urgency = ReferralUrgency.Regular
        };
    }
}
