using Server.Models.Enums;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Repositories;
using Server.Repositories.InterFaces;
using Server.Models;
using Server.Services;
using Server.Services.InterFaces;
using Server.Tests.Fixtures;

namespace Server.Tests.Tests.Unit
{
    [TestClass]
    [TestCategory("Unit")]
    public class ReferralServiceTests
    {
        [TestMethod]
        public async Task ValidRequestIsSavedAsOpenWithServerGeneratedIdentityAndTime()
        {
            var repository = new RecordingReferralRepository();
            var service = new ReferralService(repository, new StubTreatmentCatalogRepository());
            var request = new ReferralRequests().Valid();
            var before = DateTimeOffset.UtcNow;
            using var cancellation = new CancellationTokenSource();

            var result = await service.CreateAsync(request, cancellation.Token);

            Assert.IsEmpty(result.Errors);
            Assert.IsNotNull(result.Referral);
            Assert.HasCount(1, repository.Saved);
            var saved = repository.Saved.Single();
            Assert.AreNotEqual(Guid.Empty, saved.Id);
            Assert.AreEqual(ReferralStatus.Open, saved.Status);
            Assert.IsTrue(saved.CreatedAtUtc >= before && saved.CreatedAtUtc <= DateTimeOffset.UtcNow);
            Assert.AreEqual(TimeSpan.Zero, saved.CreatedAtUtc.Offset);
            Assert.AreEqual(saved.Id, result.Referral.Id);
            Assert.AreEqual("Open", result.Referral.Status);
            Assert.AreEqual(saved.CreatedAtUtc, result.Referral.CreatedAtUtc);
            Assert.AreEqual(request.Bsn, saved.Bsn);
            Assert.AreEqual(request.MedicalInformation, saved.MedicalInformation);
            Assert.IsEmpty(saved.Attachments);
            Assert.AreEqual(cancellation.Token, repository.ReceivedCancellationToken);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" \t\r\n")]
        public async Task MissingKeyFieldsReturnExactMessagesWithoutSaving(string? value)
        {
            var repository = new RecordingReferralRepository();
            var request = new ReferralRequests().Valid();
            request.Bsn = value;
            request.Specialty = value;
            request.TreatmentCode = value;

            var result = await new ReferralService(repository, new StubTreatmentCatalogRepository()).CreateAsync(request);

            Assert.HasCount(3, result.Errors);
            CollectionAssert.AreEqual(new[] { "Vul het BSN in" }, result.Errors["bsn"]);
            CollectionAssert.AreEqual(new[] { "Kies een specialisme" }, result.Errors["specialty"]);
            CollectionAssert.AreEqual(new[] { "Kies een behandelcode" }, result.Errors["treatmentCode"]);
            Assert.IsNull(result.Referral);
            Assert.IsEmpty(repository.Saved);
        }

        [TestMethod]
        [DataRow(nameof(CreateReferralRequest.PatientName), "patientName")]
        [DataRow(nameof(CreateReferralRequest.PatientPostalCode), "patientPostalCode")]
        [DataRow(nameof(CreateReferralRequest.PatientHouseNumber), "patientHouseNumber")]
        [DataRow(nameof(CreateReferralRequest.PatientEmailAddress), "patientEmailAddress")]
        [DataRow(nameof(CreateReferralRequest.ReasonForReferral), "reasonForReferral")]
        [DataRow(nameof(CreateReferralRequest.Medication), "medication")]
        [DataRow(nameof(CreateReferralRequest.MedicalInformation), "medicalInformation")]
        [DataRow(nameof(CreateReferralRequest.Allergies), "allergies")]
        public async Task OtherRequiredFieldsCannotBeBlank(string propertyName, string errorKey)
        {
            var repository = new RecordingReferralRepository();
            var request = new ReferralRequests().Valid();
            typeof(CreateReferralRequest).GetProperty(propertyName)!.SetValue(request, " \t ");

            var result = await new ReferralService(repository, new StubTreatmentCatalogRepository()).CreateAsync(request);

            Assert.IsTrue(result.Errors.ContainsKey(errorKey));
            Assert.IsEmpty(repository.Saved);
        }

        [TestMethod]
        [DataRow(nameof(CreateReferralRequest.Bsn), "bsn", 9)]
        [DataRow(nameof(CreateReferralRequest.PatientName), "patientName", 200)]
        [DataRow(nameof(CreateReferralRequest.PatientPostalCode), "patientPostalCode", 12)]
        [DataRow(nameof(CreateReferralRequest.PatientHouseNumber), "patientHouseNumber", 20)]
        [DataRow(nameof(CreateReferralRequest.PatientPhoneNumber), "patientPhoneNumber", 32)]
        [DataRow(nameof(CreateReferralRequest.PatientEmailAddress), "patientEmailAddress", 254)]
        public async Task ColumnLengthLimitIsAcceptedButLongerValuesAreRejected(
            string propertyName, string errorKey, int limit)
        {
            var repository = new RecordingReferralRepository();
            var service = new ReferralService(repository, new StubTreatmentCatalogRepository());
            var request = new ReferralRequests().Valid();
            var property = typeof(CreateReferralRequest).GetProperty(propertyName)!;
            property.SetValue(request, new string('1', limit + 1));

            var rejected = await service.CreateAsync(request);

            Assert.IsTrue(rejected.Errors.ContainsKey(errorKey));
            Assert.IsEmpty(repository.Saved);
            property.SetValue(request, new string('1', limit));

            var accepted = await service.CreateAsync(request);

            Assert.IsEmpty(accepted.Errors);
            Assert.HasCount(1, repository.Saved);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(0)]
        [DataRow(999)]
        public async Task MissingOrUnknownUrgencyIsRejected(int? urgency)
        {
            var repository = new RecordingReferralRepository();
            var request = new ReferralRequests().Valid();
            request.Urgency = urgency.HasValue ? (ReferralUrgency)urgency.Value : null;

            var result = await new ReferralService(repository, new StubTreatmentCatalogRepository()).CreateAsync(request);

            Assert.IsTrue(result.Errors.ContainsKey("urgency"));
            Assert.IsEmpty(repository.Saved);
        }

        [TestMethod]
        [DataRow(nameof(CreateReferralRequest.Specialty), "specialty", 100)]
        [DataRow(nameof(CreateReferralRequest.TreatmentCode), "treatmentCode", 50)]
        public async Task OverlongCatalogueFieldsAreRejected(string propertyName, string errorKey, int limit)
        {
            var repository = new RecordingReferralRepository();
            var request = new ReferralRequests().Valid();
            typeof(CreateReferralRequest).GetProperty(propertyName)!.SetValue(request, new string('x', limit + 1));

            var result = await new ReferralService(repository, new StubTreatmentCatalogRepository()).CreateAsync(request);

            Assert.IsTrue(result.Errors.ContainsKey(errorKey));
            Assert.IsEmpty(repository.Saved);
        }

        [TestMethod]
        [DataRow("Z9999", "KNO", "Kies een geldige behandelcode")]
        [DataRow("Z0001", "KNO", "De behandelcode hoort niet bij het gekozen specialisme")]
        [DataRow("Z0005", "Onbekend", "De behandelcode hoort niet bij het gekozen specialisme")]
        public async Task UnknownOrMismatchedCatalogueChoicesAreRejectedWithoutSaving(
            string code, string specialty, string message)
        {
            var repository = new RecordingReferralRepository();
            var request = new ReferralRequests().Valid();
            request.TreatmentCode = code;
            request.Specialty = specialty;

            var result = await new ReferralService(repository, new StubTreatmentCatalogRepository()).CreateAsync(request);

            CollectionAssert.AreEqual(new[] { message }, result.Errors["treatmentCode"]);
            Assert.IsNull(result.Referral);
            Assert.IsEmpty(repository.Saved);
        }

        [TestMethod]
        public async Task ValidChoicesAreStoredUsingTheCatalogueSpelling()
        {
            var repository = new RecordingReferralRepository();
            var request = new ReferralRequests().Valid();
            request.TreatmentCode = " z0005 ";
            request.Specialty = " kno ";

            var result = await new ReferralService(repository, new StubTreatmentCatalogRepository()).CreateAsync(request);

            Assert.IsEmpty(result.Errors);
            Assert.AreEqual("Z0005", repository.Saved.Single().TreatmentCode);
            Assert.AreEqual("KNO", repository.Saved.Single().Specialty);
        }

        [TestMethod]
        public async Task CancelledRequestDoesNotSaveAReferral()
        {
            var repository = new RecordingReferralRepository();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
                new ReferralService(repository, new StubTreatmentCatalogRepository())
                    .CreateAsync(new ReferralRequests().Valid(), cancellation.Token));

            Assert.IsEmpty(repository.Saved);
        }

        private class RecordingReferralRepository : IReferralRepository
        {
            public List<Referral> Saved { get; } = [];
            public CancellationToken ReceivedCancellationToken { get; private set; }

            public Task AddAsync(Referral referral, CancellationToken cancellationToken)
            {
                Saved.Add(referral);
                ReceivedCancellationToken = cancellationToken;
                return Task.CompletedTask;
            }
        }
    }
}
