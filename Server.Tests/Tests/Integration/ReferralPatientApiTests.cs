using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Server.Data;
using Server.Models.DTO.Responses;
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
        using var client = factory.CreateClient();
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
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
    public async Task AnyGpCanFindAnExactBsnAndOnlyReceivesPatientContactDetails()
    {
        using var fixture = new ReferralApiFactory();
        using var factory = fixture.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.AddTransient<IStartupFilter, ReferralLookupTestPrincipal>()));
        using var client = factory.CreateClient();
        var referral = new ReferralRequests().Valid();
        referral.Bsn = "012345678";
        using var created = await client.PostAsJsonAsync("/api/referrals", referral);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);

        using var anonymous = await client.PostAsJsonAsync(LookupUrl, new { bsn = referral.Bsn });
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-Role", "Patient");
        using var otherRole = await client.PostAsJsonAsync(LookupUrl, new { bsn = referral.Bsn });
        Assert.AreEqual(HttpStatusCode.Forbidden, otherRole.StatusCode);
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Add("X-Test-Role", "Huisarts");

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

    // Supplies a principal exclusively in the test host, ahead of routing.
    private class ReferralLookupTestPrincipal : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, proceed) =>
            {
                if (context.Request.Headers.TryGetValue("X-Test-Role", out var role))
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.Role, role.ToString())], "ReferralLookupTests"));
                }
                await proceed(context);
            });
            next(app);
        };
    }
}
