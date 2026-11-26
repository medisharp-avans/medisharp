using Moq;
using Server.Models.DTO.Common;
using Server.Models.DTO.Responses;
using Server.Repositories.InterFaces;
using Server.Services;

namespace Server.Tests.Tests.Unit
{
    /// <summary>
    /// Covers the paging numbers of the claims overview (DEC-01). The search and sort themselves
    /// are tested against the database in ClaimRepositoryTests.
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

        [TestMethod]
        public async Task GetOverviewPageAsync_ReturnsPagingNumbers()
        {
            var query = new PagedQuery { Page = 2, PageSize = 10 };
            List<ClaimOverviewDTO> items = [new() { Id = 11 }, new() { Id = 12 }];
            _repositoryMock.Setup(r => r.GetOverviewPageAsync(query)).ReturnsAsync((items, 12));

            var result = await _sut.GetOverviewPageAsync(query);

            Assert.AreSame(items, result.Items);
            Assert.AreEqual(2, result.Page);
            Assert.AreEqual(10, result.PageSize);
            Assert.AreEqual(12, result.TotalCount);
            Assert.AreEqual(2, result.TotalPages);
        }

        [TestMethod]
        public async Task GetOverviewPageAsync_NoClaims_ReturnsZeroPages()
        {
            var query = new PagedQuery();
            List<ClaimOverviewDTO> noItems = [];
            _repositoryMock.Setup(r => r.GetOverviewPageAsync(query)).ReturnsAsync((noItems, 0));

            var result = await _sut.GetOverviewPageAsync(query);

            Assert.IsEmpty(result.Items);
            Assert.AreEqual(0, result.TotalPages);
        }

        [TestMethod]
        public async Task CountToReviewAsync_ReturnsRepositoryResult()
        {
            _repositoryMock.Setup(r => r.CountToReviewAsync()).ReturnsAsync(3);

            Assert.AreEqual(3, await _sut.CountToReviewAsync());
        }
    }
}
