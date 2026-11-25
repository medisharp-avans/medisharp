using System.Net;
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
        Assert.IsTrue(response.Headers.CacheControl?.NoStore);
    }
}
