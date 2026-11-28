using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Server.Data;
using Server.Models;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Services.InterFaces;
using Server.Tests.Fixtures;

namespace Server.Tests.Tests.Integration;

[TestClass]
[TestCategory("Integration")]
public class ReferralPatientApiTests
{
    private const string LookupUrl = "/api/referrals/patient-lookup";

    [TestMethod]
    public async Task NewAndExistingPatientsAreLinkedToTheSamePatientRecord()
    {
        using var factory = new ReferralApiFactory();
        using var client = ReferralApiFactory.CreateClientAs(factory);
        var first = new ReferralRequests().Valid();
        first.PatientPhoneNumber = null;
        var second = new ReferralRequests().Valid();
        second.PatientPostalCode = "5678 CD";

        using var firstResponse = await client.PostAsJsonAsync("/api/referrals", first);
        using var secondResponse = await client.PostAsJsonAsync("/api/referrals", second);

        Assert.AreEqual(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, secondResponse.StatusCode);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<CreatedReferralResponse>();
        Assert.IsNotNull(secondResult);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ReferralDbContext>();
        var patient = await context.Patients.SingleAsync();
        var referrals = await context.Referrals.AsNoTracking().ToListAsync();
        Assert.HasCount(2, referrals);
        Assert.IsTrue(referrals.All(referral => referral.PatientId == patient.Id));
        Assert.AreEqual(first.Bsn, patient.Bsn);
        Assert.AreEqual(first.PatientPostalCode, patient.PostalCode);
        Assert.IsNull(patient.PhoneNumber);
        Assert.AreEqual(second.PatientPostalCode, referrals.Single(referral => referral.Id == secondResult.Id).PatientPostalCode);
    }

    [TestMethod]
    public async Task GpCanCreateAndLookUpAReferralAfterEmailVerification()
    {
        using var fixture = new ReferralApiFactory();
        var mailbox = new TestMailbox();
        using var factory = fixture.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService>(_ => mailbox);
        }));
        using var client = factory.CreateClient();
        using var otherRole = ReferralApiFactory.CreateClientAs(factory, UserRole.Patient);
        var referral = new ReferralRequests().Valid();
        referral.Bsn = "012345678";

        const string email = "huisarts@example.test";
        const string password = "FictiefTestwachtwoord!";
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var user = new User { Email = email, Role = UserRole.Huisarts, FullName = "Testhuisarts" };
            user.PasswordHash = hasher.HashPassword(user, password);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        using var login = await client.PostAsJsonAsync("/api/login", new { email, password });
        Assert.AreEqual(HttpStatusCode.OK, login.StatusCode);
        var challenge = await login.Content.ReadFromJsonAsync<LoginChallengeDTO>();
        Assert.IsNotNull(challenge);
        using var beforeVerification = await client.PostAsJsonAsync("/api/referrals", referral);
        Assert.AreEqual(HttpStatusCode.Unauthorized, beforeVerification.StatusCode);

        using var verification = await client.PostAsJsonAsync("/api/login/verify",
            new { challengeId = challenge.ChallengeId, code = mailbox.Code });
        Assert.AreEqual(HttpStatusCode.OK, verification.StatusCode);
        var session = await verification.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.IsNotNull(session);
        Assert.AreEqual(UserRole.Huisarts, session.User.Role);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.Token);
        using var created = await client.PostAsJsonAsync("/api/referrals", referral);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);

        using var renewed = await client.PostAsJsonAsync("/api/login/refresh", new { session.RefreshToken });
        Assert.AreEqual(HttpStatusCode.OK, renewed.StatusCode);
        var refreshedSession = await renewed.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.IsNotNull(refreshedSession);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshedSession.Token);

        using var denied = await otherRole.PostAsJsonAsync(LookupUrl, new { bsn = referral.Bsn });
        Assert.AreEqual(HttpStatusCode.Forbidden, denied.StatusCode);

        using var match = await client.PostAsJsonAsync(LookupUrl, new { bsn = referral.Bsn });
        Assert.AreEqual(HttpStatusCode.OK, match.StatusCode);
        Assert.IsTrue(match.Headers.CacheControl?.NoStore);
        using var json = JsonDocument.Parse(await match.Content.ReadAsStringAsync());
        var patient = json.RootElement;
        CollectionAssert.AreEquivalent(new[] { "patientName", "patientPostalCode", "patientHouseNumber", "patientPhoneNumber", "patientEmailAddress" },
            patient.EnumerateObject().Select(field => field.Name).ToArray());
        Assert.AreEqual(referral.PatientName, patient.GetProperty("patientName").GetString());
        Assert.AreEqual(referral.PatientPostalCode, patient.GetProperty("patientPostalCode").GetString());
        Assert.AreEqual(referral.PatientHouseNumber, patient.GetProperty("patientHouseNumber").GetString());
        Assert.AreEqual(referral.PatientPhoneNumber, patient.GetProperty("patientPhoneNumber").GetString());
        Assert.AreEqual(referral.PatientEmailAddress, patient.GetProperty("patientEmailAddress").GetString());

        using var absent = await client.PostAsJsonAsync(LookupUrl, new { bsn = "012345679" });
        Assert.AreEqual(HttpStatusCode.NoContent, absent.StatusCode);
        using var partial = await client.PostAsJsonAsync(LookupUrl, new { bsn = "01234" });
        Assert.AreEqual(HttpStatusCode.BadRequest, partial.StatusCode);
    }

    private class TestMailbox : IEmailService
    {
        public string Code { get; private set; } = string.Empty;

        public Task SendAsync(string to, string subject, string body)
        {
            Code = Regex.Match(body, @"\b[0-9]{6}\b").Value;
            return Task.CompletedTask;
        }
    }
}
