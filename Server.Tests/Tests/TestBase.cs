using Server.Tests.Utils;
using System.Diagnostics;
using System.Reflection;

namespace Server.Tests
{
    /// <summary>
    /// Base of all tests: logs the start and the result of every test, with its TestCategory (the TC number).
    /// Unit tests inherit from this class directly, so they need no database, app or browser.
    /// </summary>
    public abstract class TestBase
    {
        // MSTest fills this in automatically; it knows the name and result of the running test
        public TestContext TestContext { get; set; } = null!;

        private Stopwatch _stopwatch = null!;

        [TestInitialize]
        public void LogStart()
        {
            _stopwatch = Stopwatch.StartNew();
            TestLog.Logger.Information("▶ START  {Category} {Test}", GetTestCategories(), TestContext.TestName);
        }

        [TestCleanup]
        public void LogEnd()
        {
            TestLog.Logger.Information("■ END    {Category} {Test} => {Outcome} ({Ms} ms) {Screenshot}",
                GetTestCategories(), TestContext.TestName, TestContext.CurrentTestOutcome,
                _stopwatch.ElapsedMilliseconds, GetFailureEvidence());
        }

        /// <summary>Extra evidence for a failed test, such as a screenshot. Empty for unit tests.</summary>
        protected virtual string? GetFailureEvidence() => null;

        // The TC numbers of the running test, from its [TestCategory] attributes
        private string GetTestCategories()
        {
            var method = GetType().GetMethod(TestContext.TestName!);
            return string.Join(", ", method?.GetCustomAttributes<TestCategoryAttribute>().SelectMany(a => a.TestCategories) ?? []);
        }
    }
}
