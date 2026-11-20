using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Server.Contracts.Referrals;
using Server.Contracts.Treatments;
using Server.Data;
using Server.Tests.Fixtures;
using System.Net;
using System.Net.Http.Json;

namespace Server.Tests.Tests.Integration
{
    /// <summary>Exercises catalogue choices and referral validation through the HTTP API and database.</summary>
    [TestClass]
    [TestCategory("Integration")]
    public class TreatmentCatalogApiTests
    {
        [TestMethod]
        public async Task EveryCatalogueTreatmentIsReachableThroughItsSpecialty()
        {
            using var factory = new ReferralApiFactory();
            using var client = factory.CreateClient();

            var specialties = await client.GetFromJsonAsync<string[]>("/api/treatments/specialties");

            Assert.IsNotNull(specialties);
            Assert.HasCount(12, specialties);
            CollectionAssert.AreEqual(new[] { "Algemeen", "Cardiologie", "Chirurgie", "Dermatologie", "KNO",
                "Laboratoriumgeneeskunde", "Longgeneeskunde", "Oogheelkunde", "Orthopedie", "Psychiatrie",
                "Radiologie", "Revalidatiegeneeskunde" }, specialties);
            var treatments = new List<TreatmentResponse>();
            foreach (var specialty in specialties)
            {
                var choices = await client.GetFromJsonAsync<TreatmentResponse[]>(
                    $"/api/treatments?specialty={Uri.EscapeDataString(specialty)}");
                Assert.IsNotNull(choices);
                Assert.IsNotEmpty(choices);
                Assert.IsTrue(choices.All(treatment => treatment.Specialty == specialty));
                treatments.AddRange(choices);
            }

            Assert.HasCount(100, treatments);
            Assert.HasCount(100, treatments.Select(treatment => treatment.Code).Distinct().ToArray());
            Assert.IsTrue(treatments.All(treatment => treatment.DurationInQuarters > 0 && treatment.CostEuro >= 0));
            var first = treatments.Single(treatment => treatment.Code == "Z0001");
            Assert.AreEqual("Consult hart", first.Description);
            Assert.AreEqual(3, first.DurationInQuarters);
            Assert.AreEqual(144m, first.CostEuro);
            var last = treatments.Single(treatment => treatment.Code == "Z0100");
            Assert.AreEqual("Bloedonderzoek infecties", last.Description);
            Assert.AreEqual(4, last.DurationInQuarters);
            Assert.AreEqual(37m, last.CostEuro);
        }

        [TestMethod]
        public async Task EqualDescriptionsRetainDistinctCodesDurationsAndCosts()
        {
            using var factory = new ReferralApiFactory();
            using var client = factory.CreateClient();

            var choices = await client.GetFromJsonAsync<TreatmentResponse[]>("/api/treatments?specialty=KNO");

            Assert.IsNotNull(choices);
            CollectionAssert.AreEqual(new[] { "Z0005", "Z0072", "Z0015", "Z0096" },
                choices.Select(treatment => treatment.Code).ToArray());
            var consultations = choices.Where(treatment => treatment.Description == "Consult oren").ToArray();
            Assert.HasCount(2, consultations);
            Assert.AreEqual(3, consultations[0].DurationInQuarters);
            Assert.AreEqual(65m, consultations[0].CostEuro);
            Assert.AreEqual(4, consultations[1].DurationInQuarters);
            Assert.AreEqual(110m, consultations[1].CostEuro);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("?specialty=")]
        [DataRow("?specialty=%20%20")]
        public async Task MissingSpecialtyReturnsAFieldError(string query)
        {
            using var factory = new ReferralApiFactory();
            using var client = factory.CreateClient();

            using var response = await client.GetAsync("/api/treatments" + query);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            Assert.IsNotNull(problem);
            Assert.IsTrue(problem.Errors.Values.SelectMany(messages => messages).Contains("Kies een specialisme"));
        }

        [TestMethod]
        public async Task OverlongSpecialtyIsRejected()
        {
            using var factory = new ReferralApiFactory();
            using var client = factory.CreateClient();

            using var response = await client.GetAsync("/api/treatments?specialty=" + new string('x', 101));

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task UnknownSpecialtyDoesNotReturnOtherTreatments()
        {
            using var factory = new ReferralApiFactory();
            using var client = factory.CreateClient();

            var choices = await client.GetFromJsonAsync<TreatmentResponse[]>("/api/treatments?specialty=Onbekend");

            Assert.IsNotNull(choices);
            Assert.IsEmpty(choices);
        }

        [TestMethod]
        [DataRow("Z9999", "KNO", "Kies een geldige behandelcode")]
        [DataRow("Z0001", "KNO", "De behandelcode hoort niet bij het gekozen specialisme")]
        [DataRow("Z0005", "Onbekend", "De behandelcode hoort niet bij het gekozen specialisme")]
        public async Task InvalidChoiceDoesNotPersistAndACorrectedChoiceCanBeSaved(
            string code, string specialty, string message)
        {
            using var factory = new ReferralApiFactory();
            using var client = factory.CreateClient();
            var request = new ReferralRequests().Valid();
            request.TreatmentCode = code;
            request.Specialty = specialty;

            using var rejected = await client.PostAsJsonAsync("/api/referrals", request);

            Assert.AreEqual(HttpStatusCode.BadRequest, rejected.StatusCode);
            var problem = await rejected.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            Assert.IsNotNull(problem);
            CollectionAssert.AreEqual(new[] { message }, problem.Errors["treatmentCode"]);
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.AreEqual(0, await context.Referrals.CountAsync());

            using var accepted = await client.PostAsJsonAsync("/api/referrals", new ReferralRequests().Valid());
            Assert.AreEqual(HttpStatusCode.Created, accepted.StatusCode);
            var confirmation = await accepted.Content.ReadFromJsonAsync<CreatedReferralResponse>();
            Assert.IsNotNull(confirmation);
            var saved = await context.Referrals.AsNoTracking().SingleAsync();
            Assert.AreEqual(confirmation.Id, saved.Id);
            Assert.AreEqual("Z0005", saved.TreatmentCode);
            Assert.AreEqual("KNO", saved.Specialty);
        }

        [TestMethod]
        [DataRow("Staging")]
        [DataRow("Production")]
        public async Task ReferenceDataRemainsReadableWhenReferralCreationIsDisabled(string environment)
        {
            using var factory = new ReferralApiFactory(environment);
            using var client = factory.CreateClient();

            using var response = await client.GetAsync("/api/treatments/specialties");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            using var referralResponse = await client.PostAsJsonAsync("/api/referrals", new ReferralRequests().Valid());
            Assert.AreEqual(HttpStatusCode.NotFound, referralResponse.StatusCode);
        }
    }
}
