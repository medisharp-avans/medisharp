using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Server.Tests.Utils;

namespace Server.Tests
{
    /// <summary>
    /// Base of end-to-end tests: a fresh test database, the API and client running, and a new Chrome browser per test.
    /// </summary>
    public abstract class EndToEndTestBase : DatabaseTestBase
    {
        private string? _failureScreenshot;

        /// <summary>The Chrome browser of this test.</summary>
        protected IWebDriver Driver { get; private set; } = null!;

        /// <summary>Makes sure the API and client run, then opens a new Chrome browser.</summary>
        [TestInitialize]
        public async Task StartBrowserAsync()
        {
            // Starts the API and client once for the whole test run, after the first database reset
            await AppServer.EnsureStartedAsync();

            var options = new ChromeOptions { AcceptInsecureCertificates = true };
            // The tests only open localhost, so Chrome does not need to look up proxy settings (saves about a second per browser)
            options.AddArgument("--no-proxy-server");
            // A fixed size instead of maximizing, so the layout is the same on every computer and in headless mode
            options.AddArgument("--window-size=1920,1080");
            if (TestSettings.Headless)
                options.AddArgument("--headless=new");

            // Selenium has no async API, so the browser is started and stopped synchronously
            Driver = new ChromeDriver(options);
        }

        /// <summary>
        /// Takes a screenshot when the test failed and closes the browser.
        /// MSTest runs this cleanup before the one of TestBase, so the screenshot is taken while the browser is still open.
        /// </summary>
        [TestCleanup]
        public void StopBrowser()
        {
            if (TestContext.CurrentTestOutcome != UnitTestOutcome.Passed)
                _failureScreenshot = TestLog.FailureScreenshot(Driver, TestContext.TestName!);

            Driver?.Quit();
            Driver?.Dispose();
        }

        /// <inheritdoc />
        protected override string? GetFailureEvidence() => _failureScreenshot;
    }
}
