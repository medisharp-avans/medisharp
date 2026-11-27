using System.Net;
using System.Net.Http.Json;
using Server.Models.Enums;
using Server.Models.DTO.Responses;
using Server.Tests.Fixtures;

namespace Server.Tests.Tests.Integration;

[TestClass]
[TestCategory("Integration")]
public class OpenReferralAccessTests
{
    [TestMethod]
    public async Task AnonymousRequestsCannotReadOpenReferrals()
    {
        using var factory = new ReferralApiFactory();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/referrals/open");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task HospitalStaffCanReadTheOpenReferralCreatedByTheGp()
    {
        using var factory = new ReferralApiFactory();
        using var gp = ReferralApiFactory.CreateClientAs(factory);
        using var staff = ReferralApiFactory.CreateClientAs(factory, UserRole.Ziekenhuismedewerker);
        var referral = new ReferralRequests().Valid();

        using var forbidden = await staff.PostAsJsonAsync("/api/referrals", referral);
        Assert.AreEqual(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using var created = await gp.PostAsJsonAsync("/api/referrals", referral);
        Assert.AreEqual(HttpStatusCode.Created, created.StatusCode);

        using var denied = await gp.GetAsync("/api/referrals/open");
        Assert.AreEqual(HttpStatusCode.Forbidden, denied.StatusCode);
        using var open = await staff.GetAsync("/api/referrals/open");
        Assert.AreEqual(HttpStatusCode.OK, open.StatusCode);
        Assert.IsTrue(open.Headers.CacheControl?.NoStore);
        var referrals = await open.Content.ReadFromJsonAsync<List<OpenReferralResponse>>();
        Assert.IsNotNull(referrals);
        Assert.AreEqual(referral.Bsn, referrals.Single().Bsn);
    }
}
