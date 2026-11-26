using Microsoft.EntityFrameworkCore;
using Server.Models.DTO.Common;
using Server.Repositories;
using Server.Tests.TestDb;

namespace Server.Tests.Tests.Integration
{
    /// <summary>
    /// Covers the search, sort and paging of the claims overview (DEC-01) against the test database,
    /// with the four claims of TestDECDataInitializer (identified by their policy number).
    /// </summary>
    [TestClass]
    [TestCategory("DEC-01")]
    public class ClaimRepositoryTests : DatabaseTestBase
    {
        private ClaimRepository _sut = null!;

        protected override Func<DbContext, Task>? GetTestDataInitializer()
        {
            return TestDECDataInitializer.SeedAsync;
        }

        [TestInitialize]
        public void CreateSut()
        {
            _sut = new ClaimRepository(DbContext);
        }

        private async Task<string[]> PolicyNumbersAsync(PagedQuery query)
        {
            var (items, _) = await _sut.GetOverviewPageAsync(query);
            return [.. items.Select(i => i.PolicyNumber)];
        }

        [TestMethod]
        public async Task GetOverviewPageAsync_CountsAndSumsTheLines_AndLeavesReviewerEmptyWhenNotReviewed()
        {
            // TC-DEC-01_2_2: 300 + 120 = 420.
            var (items, _) = await _sut.GetOverviewPageAsync(new PagedQuery());
            var row = items.Single(i => i.PolicyNumber == "POLIS-BBB");

            Assert.AreEqual(2, row.LineCount);
            Assert.AreEqual(420m, row.TotalAmount);
            Assert.AreEqual("Emma de Vries", row.PatientName);
            Assert.IsNull(row.ReviewedByName);
            Assert.AreEqual("Sanne Medewerker", items.Single(i => i.PolicyNumber == "POLIS-AAA").ReviewedByName);
        }

        [TestMethod]
        public async Task GetOverviewPageAsync_WithoutSortColumn_PutsToReviewClaimsFirst()
        {
            string[] expected = ["POLIS-DDD", "POLIS-BBB", "POLIS-AAA", "POLIS-CCC"];
            CollectionAssert.AreEqual(expected, await PolicyNumbersAsync(new PagedQuery()));
        }

        [TestMethod]
        public async Task GetOverviewPageAsync_SortOnPatientDescending_SortsByNameThenClaimNumber()
        {
            string[] expected = ["POLIS-CCC", "POLIS-AAA", "POLIS-DDD", "POLIS-BBB"];
            CollectionAssert.AreEqual(expected, await PolicyNumbersAsync(new PagedQuery
            {
                SortBy = "Patient",
                SortDescending = true
            }));
        }

        [TestMethod]
        public async Task GetOverviewPageAsync_SortOnStatus_UsesTheWorkflowOrder()
        {
            string[] expected = ["POLIS-BBB", "POLIS-DDD", "POLIS-CCC", "POLIS-AAA"];
            CollectionAssert.AreEqual(expected, await PolicyNumbersAsync(new PagedQuery { SortBy = "status" }));
        }

        [TestMethod]
        public async Task GetOverviewPageAsync_SortOnTotalAmount_SortsOnTheSumOfTheLines()
        {
            string[] expected = ["POLIS-DDD", "POLIS-CCC", "POLIS-AAA", "POLIS-BBB"];
            CollectionAssert.AreEqual(expected, await PolicyNumbersAsync(new PagedQuery { SortBy = "TotalAmount" }));
        }

        [TestMethod]
        [DataRow("jansen", new[] { "POLIS-DDD", "POLIS-AAA" })]
        [DataRow("POLIS-CCC", new[] { "POLIS-CCC" })]
        [DataRow("sanne", new[] { "POLIS-AAA", "POLIS-CCC" })]
        [DataRow("   ", new[] { "POLIS-DDD", "POLIS-BBB", "POLIS-AAA", "POLIS-CCC" })]
        [DataRow("bestaat niet", new string[0])]
        public async Task GetOverviewPageAsync_SearchesPatientPolicyAndReviewer_CaseInsensitive(string search, string[] expected)
        {
            CollectionAssert.AreEqual(expected, await PolicyNumbersAsync(new PagedQuery { Search = search }));
        }

        [TestMethod]
        public async Task GetOverviewPageAsync_SearchOnClaimNumber_FindsThatClaim()
        {
            var claimId = await DbContext.Claims
                .Where(c => c.PolicyNumber == "POLIS-BBB")
                .Select(c => c.Id)
                .SingleAsync(TestContext.CancellationToken);

            string[] expected = ["POLIS-BBB"];
            CollectionAssert.AreEqual(expected, await PolicyNumbersAsync(new PagedQuery { Search = $"#{claimId}" }));
        }

        [TestMethod]
        public async Task GetOverviewPageAsync_ReturnsTheRequestedPageAndTheCounts()
        {
            var (items, totalCount) = await _sut.GetOverviewPageAsync(new PagedQuery { Page = 2, PageSize = 3 });

            Assert.HasCount(1, items);
            Assert.AreEqual("POLIS-CCC", items[0].PolicyNumber);
            Assert.AreEqual(4, totalCount);
        }

        [TestMethod]
        public async Task GetOverviewPageAsync_Search_CountsOnlyTheMatches()
        {
            var (_, totalCount) = await _sut.GetOverviewPageAsync(new PagedQuery { Search = "Piet" });

            Assert.AreEqual(1, totalCount);
        }

        [TestMethod]
        public async Task GetOverviewPageAsync_UnknownSortColumn_UsesTheDefaultOrder()
        {
            string[] expected = ["POLIS-DDD", "POLIS-BBB", "POLIS-AAA", "POLIS-CCC"];
            CollectionAssert.AreEqual(expected, await PolicyNumbersAsync(new PagedQuery { SortBy = "bestaat-niet" }));
        }

        [TestMethod]
        public async Task CountToReviewAsync_CountsAllClaimsToReview()
        {
            Assert.AreEqual(2, await _sut.CountToReviewAsync());
        }
    }
}
