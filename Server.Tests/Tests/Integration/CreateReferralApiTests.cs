using Server.Models.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Data;
using Server.Repositories;
using Server.Repositories.InterFaces;
using Server.Models;
using Server.Tests.Fixtures;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Server.Tests.Tests.Integration
{
    [TestClass]
    [TestCategory("Integration")]
    public class CreateReferralApiTests
    {
        [TestMethod]
        public async Task PostPersistsSelectedDataAndReturnsOnlyAConfirmation()
        {
            using var factory = new ReferralApiFactory();
            using var client = factory.CreateClient();
            var request = new ReferralRequests().Valid();

            using var response = await client.PostAsJsonAsync("/api/referrals", request);

            Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<CreatedReferralResponse>();
            Assert.IsNotNull(result);
            Assert.AreEqual("Open", result.Status);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            CollectionAssert.AreEquivalent(new[] { "id", "status", "createdAtUtc" },
                json.RootElement.EnumerateObject().Select(property => property.Name).ToArray());

            // Use a fresh context so this proves persistence, not just EF tracking.
            using var scope = factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var saved = await context.Referrals.AsNoTracking().SingleAsync();
            Assert.AreEqual(result.Id, saved.Id);
            Assert.AreEqual(ReferralStatus.Open, saved.Status);
            Assert.AreEqual(result.CreatedAtUtc, saved.CreatedAtUtc);
            Assert.AreEqual(request.Bsn, saved.Bsn);
            Assert.AreEqual(request.PatientName, saved.PatientName);
            Assert.AreEqual(request.PatientAddress, saved.PatientAddress);
            Assert.AreEqual(request.PatientCity, saved.PatientCity);
            Assert.AreEqual(request.PatientPhoneNumber, saved.PatientPhoneNumber);
            Assert.AreEqual(request.PatientEmailAddress, saved.PatientEmailAddress);
            Assert.AreEqual(request.Specialty, saved.Specialty);
            Assert.AreEqual(request.TreatmentCode, saved.TreatmentCode);
            Assert.AreEqual(request.ReasonForReferral, saved.ReasonForReferral);
            Assert.AreEqual(request.ClinicalQuestion, saved.ClinicalQuestion);
            Assert.AreEqual(request.Medication, saved.Medication);
            Assert.AreEqual(request.MedicalInformation, saved.MedicalInformation);
            Assert.AreEqual(request.Allergies, saved.Allergies);
            Assert.AreEqual(request.Urgency, saved.Urgency);
            Assert.AreEqual(0, await context.ReferralAttachments.CountAsync());
        }

        [TestMethod]
        [DataRow("bsn", "Vul het BSN in")]
        [DataRow("specialty", "Kies een specialisme")]
        [DataRow("treatmentCode", "Kies een behandelcode")]
        public async Task MissingNullEmptyAndWhitespaceFieldsReturnExactErrorWithoutSaving(
            string field, string message)
        {
            using var factory = new ReferralApiFactory();
            using var client = factory.CreateClient();

            foreach (var variant in new[] { "missing", "null", "empty", "whitespace" })
            {
                var json = ValidJson();
                if (variant == "missing")
                {
                    json.Remove(field);
                }
                else
                {
                    json[field] = variant switch { "null" => null, "empty" => "", _ => " \t\r\n" };
                }

                using var response = await client.PostAsJsonAsync("/api/referrals", json);
                Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, variant);
                var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
                Assert.IsNotNull(problem);
                CollectionAssert.AreEqual(new[] { message }, problem.Errors[field], variant);
                Assert.AreEqual(0, await CountReferralsAsync(factory), variant);
            }

            using var corrected = await client.PostAsJsonAsync("/api/referrals", new ReferralRequests().Valid());
            Assert.AreEqual(HttpStatusCode.Created, corrected.StatusCode);
            Assert.AreEqual(1, await CountReferralsAsync(factory));
        }

        [TestMethod]
        [DataRow("dossier")]
        [DataRow("status")]
        [DataRow("id")]
        [DataRow("createdAtUtc")]
        [DataRow("appointmentId")]
        [DataRow("timeSlot")]
        [DataRow("storageKey")]
        [DataRow("attachments")]
        public async Task ExtraFieldsAreRejectedWithoutSavingOrEchoingTheirContents(string field)
        {
            using var factory = new ReferralApiFactory();
            using var client = factory.CreateClient();
            const string excludedData = "NIET-MEESTUREN-AFS-01a";
            var json = ValidJson();
            json[field] = excludedData;

            using var response = await client.PostAsJsonAsync("/api/referrals", json);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsFalse((await response.Content.ReadAsStringAsync()).Contains(excludedData));
            Assert.AreEqual(0, await CountReferralsAsync(factory));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("null")]
        [DataRow("{\"bsn\":")]
        [DataRow("{\"urgency\":\"NIET-MEESTUREN-AFS-01a\"}")]
        public async Task InvalidRequestBodyIsRejectedWithoutSaving(string body)
        {
            using var factory = new ReferralApiFactory();
            using var client = factory.CreateClient();
            using var content = new StringContent(body, Encoding.UTF8, "application/json");

            using var response = await client.PostAsync("/api/referrals", content);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.IsFalse((await response.Content.ReadAsStringAsync()).Contains("NIET-MEESTUREN-AFS-01a"));
            Assert.AreEqual(0, await CountReferralsAsync(factory));
        }

        [TestMethod]
        public async Task OverlongValuesReturnValidationErrorsInsteadOfDatabaseErrors()
        {
            using var factory = new ReferralApiFactory();
            using var client = factory.CreateClient();
            var request = new ReferralRequests().Valid();
            request.Bsn = new string('1', 10);
            request.PatientName = new string('x', 201);
            request.TreatmentCode = new string('x', 51);

            using var response = await client.PostAsJsonAsync("/api/referrals", request);

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            Assert.IsNotNull(problem);
            CollectionAssert.AreEquivalent(new[] { "bsn", "patientName", "treatmentCode" }, problem.Errors.Keys.ToArray());
            Assert.AreEqual(0, await CountReferralsAsync(factory));
        }

        [TestMethod]
        [DataRow("application/json")]
        [DataRow("text/html")]
        public async Task StorageFailureReturnsSafeProblemWithoutLoggingPatientValues(string acceptedContentType)
        {
            using var logs = new CapturedLogs();
            using var databaseFactory = new ReferralApiFactory();
            using var factory = databaseFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureLogging(logging => logging.ClearProviders().AddProvider(logs));
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<IReferralRepository>();
                    services.AddScoped<IReferralRepository, FailingReferralRepository>();
                });
            });
            using var client = factory.CreateClient();
            client.DefaultRequestHeaders.Accept.ParseAdd(acceptedContentType);
            var request = new ReferralRequests().Valid();

            using var response = await client.PostAsJsonAsync("/api/referrals", request);

            Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync();
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.IsNotNull(problem);
            Assert.AreEqual(500, problem.Status);
            Assert.AreEqual("Er is iets misgegaan", problem.Title);
            Assert.AreEqual("De aanvraag kon niet worden verwerkt. Probeer het later opnieuw.", problem.Detail);
            Assert.IsTrue(problem.Extensions.ContainsKey("traceId"));
            Assert.IsTrue(logs.Messages.Any(message => message.Contains("An unexpected API error occurred.")));
            foreach (var excluded in new[] { request.Bsn!, request.PatientName!, request.MedicalInformation!,
                "FICTIEVE-INTERNE-DATABASEFOUT", nameof(DbUpdateException) })
            {
                Assert.IsFalse(body.Contains(excluded), "The response must not contain patient or exception details.");
                Assert.IsFalse(logs.Messages.Any(message => message.Contains(excluded)),
                    "Handled errors must not put patient or exception details in logs.");
            }
            Assert.AreEqual(0, await CountReferralsAsync(factory));
        }

        [TestMethod]
        [DataRow("Production")]
        [DataRow("Staging")]
        public async Task CreatingReferralsIsUnavailableOutsideDevelopmentUntilAccountsAreConnected(string environment)
        {
            using var factory = new ReferralApiFactory(environment);
            using var client = factory.CreateClient();

            using var response = await client.PostAsJsonAsync("/api/referrals", new ReferralRequests().Valid());

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.AreEqual(0, await CountReferralsAsync(factory));
        }

        private JsonObject ValidJson() => JsonSerializer.SerializeToNode(
            new ReferralRequests().Valid(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!.AsObject();

        private async Task<int> CountReferralsAsync(WebApplicationFactory<Program> factory)
        {
            using var scope = factory.Services.CreateScope();
            return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Referrals.CountAsync();
        }

        // Deliberately include fictional patient values in an exception to exercise the privacy boundary.
        private class FailingReferralRepository : IReferralRepository
        {
            public Task AddAsync(Referral referral, CancellationToken cancellationToken) =>
                throw new DbUpdateException($"{referral.Bsn} {referral.PatientName} {referral.MedicalInformation}",
                    new InvalidOperationException("FICTIEVE-INTERNE-DATABASEFOUT"));
        }

        // Per-test capture also includes exception text, which ordinary log providers would write.
        private class CapturedLogs : ILoggerProvider
        {
            public ConcurrentQueue<string> Messages { get; } = new();

            public ILogger CreateLogger(string categoryName) => new CapturedLogger(Messages);

            public void Dispose() { }

            private class CapturedLogger(ConcurrentQueue<string> messages) : ILogger
            {
                public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

                public bool IsEnabled(LogLevel logLevel) => true;

                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                    Exception? exception, Func<TState, Exception?, string> formatter) =>
                    messages.Enqueue($"{formatter(state, exception)} {exception}");
            }
        }
    }
}
