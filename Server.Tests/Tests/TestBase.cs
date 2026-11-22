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
        private Stopwatch _stopwatch = null!;

        /// <summary>Filled in by MSTest; knows the name and result of the running test.</summary>
        public TestContext TestContext { get; set; } = null!;

        /// <summary>Logs the start of the test and starts timing it.</summary>
        [TestInitialize]
        public void LogStart()
        {
            _stopwatch = Stopwatch.StartNew();
            TestLog.Logger.Information("▶ START  {Category} {Test}", GetTestCategories(), TestContext.TestName);
        }

        /// <summary>Logs the result and duration of the test, with the failure evidence if there is any.</summary>
        [TestCleanup]
        public void LogEnd()
        {
            TestLog.Logger.Information("■ END    {Category} {Test} => {Outcome} ({Ms} ms) {Screenshot}",
                GetTestCategories(), TestContext.TestName, TestContext.CurrentTestOutcome,
                _stopwatch.ElapsedMilliseconds, GetFailureEvidence());
        }

        /// <summary>Extra evidence for a failed test, such as a screenshot. Empty for unit tests.</summary>
        protected virtual string? GetFailureEvidence() => null;

        /// <summary>The TC numbers of the running test, from its [TestCategory] attributes.</summary>
        private string GetTestCategories()
        {
            var method = GetType().GetMethod(TestContext.TestName!);
            return string.Join(", ", method?.GetCustomAttributes<TestCategoryAttribute>().SelectMany(a => a.TestCategories) ?? []);
        }
    }
}
