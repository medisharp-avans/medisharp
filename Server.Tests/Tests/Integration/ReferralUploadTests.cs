using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Server.Data;
using Server.Tests.Fixtures;

namespace Server.Tests.Tests.Integration;

[TestClass]
[TestCategory("Integration")]
public class ReferralUploadTests
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task UploadPersistsBothOrNeither(bool valid)
    {
        var directory = Path.Combine(Path.GetTempPath(), "MediSharp-upload-test-" + Guid.NewGuid().ToString("N"));
        using var factory = new ReferralApiFactory().WithWebHostBuilder(builder =>
            builder.UseSetting("Attachments:StoragePath", directory));
        try
        {
            using var client = ReferralApiFactory.CreateClientAs(factory);
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(JsonSerializer.Serialize(new ReferralRequests().Valid(),
                new JsonSerializerOptions(JsonSerializerDefaults.Web))), "referral");
            var bytes = Encoding.ASCII.GetBytes(valid ? "%PDF-1.4\nFictieve test\n%%EOF" : "not a PDF");
            if (!valid) form.Add(new ByteArrayContent(Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF")), "attachments", "other.pdf");
            form.Add(new ByteArrayContent(bytes), "attachments", "test.pdf");
            using var response = await client.PostAsync("/api/referrals/with-attachments", form);
            Assert.AreEqual(valid ? HttpStatusCode.Created : HttpStatusCode.BadRequest, response.StatusCode);
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ReferralDbContext>();
            Assert.AreEqual(valid ? 1 : 0, await db.Referrals.CountAsync());
            Assert.AreEqual(valid ? 1 : 0, await db.ReferralAttachments.CountAsync());
            if (valid)
            {
                var metadata = await db.ReferralAttachments.SingleAsync();
                CollectionAssert.AreEqual(bytes, await File.ReadAllBytesAsync(Path.Combine(directory, metadata.StorageKey)));
            }
            else Assert.IsFalse(Directory.Exists(directory) && Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Any());
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [TestMethod]
    [DataRow(6, 1)]
    [DataRow(1, 11)]
    [DataRow(3, 9)]
    public async Task LimitsRejectWithoutSaving(int count, int megabytes)
    {
        using var factory = new ReferralApiFactory();
        using var client = ReferralApiFactory.CreateClientAs(factory);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(JsonSerializer.Serialize(new ReferralRequests().Valid(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))), "referral");
        for (var i = 0; i < count; i++) form.Add(new ByteArrayContent(new byte[megabytes * 1024 * 1024]), "attachments", "test.pdf");
        using var response = await client.PostAsync("/api/referrals/with-attachments", form);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.AreEqual(0, await scope.ServiceProvider.GetRequiredService<ReferralDbContext>().Referrals.CountAsync());
    }
}
