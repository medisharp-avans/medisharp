using Moq;
using Server.Models;
using Server.Models.Enums;
using Server.Repositories.InterFaces;
using Server.Services;

namespace Server.Tests.Tests.Unit
{
    /// <summary>
    /// Covers the claims overview of DEC-01: claims that still need to be reviewed come first,
    /// and each row shows the number of lines and the total amount.
    /// </summary>
    [TestClass]
    [TestCategory("DEC-01")]
    public class ClaimServiceTests : TestBase
    {
        private Mock<IClaimRepository> _repositoryMock = null!;
        private ClaimService _sut = null!;

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<IClaimRepository>();
            _sut = new ClaimService(_repositoryMock.Object);
        }

        private static Claim CreateClaim(int id, ClaimStatus status, DateTime lastModifiedAt, params decimal[] amounts) => new()
        {
            Id = id,
            PatientId = 1,
            Patient = new Patient { Id = 1, User = new User { FullName = "Jan Jansen" } },
            PolicyNumber = "ABC123456",
            Status = status,
            LastModifiedAt = lastModifiedAt,
            ClaimLines = [.. amounts.Select(a => new ClaimLine { SubmittedAmount = a })]
        };

        [TestMethod]
        public async Task GetOverviewAsync_PutsToReviewClaimsFirst()
        {
            var now = DateTime.UtcNow;
            _repositoryMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(
            [
                CreateClaim(1, ClaimStatus.Processed, now),
                CreateClaim(2, ClaimStatus.ToReview, now.AddDays(-5)),
                CreateClaim(3, ClaimStatus.ReadyToSend, now.AddDays(-1)),
                CreateClaim(4, ClaimStatus.ToReview, now.AddDays(-1))
            ]);

            var result = await _sut.GetOverviewAsync();

            int[] expectedIds = [4, 2, 1, 3];
            int[] actualIds = [.. result.Select(r => r.Id)];
            Assert.AreSequenceEqual(expectedIds, actualIds);
        }

        [TestMethod]
        public async Task GetOverviewAsync_SumsTheLineAmounts()
        {
            // TC-DEC-01_2_2: 300 + 120 = 420.
            _repositoryMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync(
            [
                CreateClaim(1, ClaimStatus.ToReview, DateTime.UtcNow, 300.00m, 120.00m)
            ]);

            var row = (await _sut.GetOverviewAsync()).Single();

            Assert.AreEqual(2, row.LineCount);
            Assert.AreEqual(420.00m, row.TotalAmount);
            Assert.AreEqual("Jan Jansen", row.PatientName);
        }

        [TestMethod]
        public async Task GetOverviewAsync_ReturnsEmptyList_WhenThereAreNoClaims()
        {
            _repositoryMock.Setup(r => r.GetAllWithDetailsAsync()).ReturnsAsync([]);

            var result = await _sut.GetOverviewAsync();

            Assert.IsEmpty(result);
        }
    }
}
