using OpenQA.Selenium;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Server.Tests.Utils
{
    /// <summary>
    /// Logger for the tests. Writes to the output of the running test, to TestLogs/TestExecutionLogs-*.txt
    /// and, for known bugs, to TestLogs/BugEvidence-*.txt. Screenshots go to TestLogs/screenshots.
    /// </summary>
    public static class TestLog
    {
        private const string Template = "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        /// <summary>The Serilog logger, for messages that are not a test step.</summary>
        public static ILogger Logger { get; }

        /// <summary>The TestLogs folder in the build output of the test project.</summary>
        public static string LogDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "TestLogs");

        static TestLog()
        {
            Directory.CreateDirectory(Path.Combine(LogDirectory, "screenshots"));

            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Sink(new CurrentConsoleSink(Template))
                .WriteTo.File(Path.Combine(LogDirectory, "TestExecutionLogs-.txt"), rollingInterval: RollingInterval.Day)
                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("BugId"))
                    .WriteTo.File(Path.Combine(LogDirectory, "BugEvidence-.txt"), rollingInterval: RollingInterval.Day))
                .CreateLogger();
        }

        /// <summary>Logs one step of a test, indented under the START line of that test.</summary>
        public static void Step(string message) => Logger.Debug("    · {Message}", message);

        /// <summary>Records that a known bug occurs: expected vs. actual, context and a screenshot.</summary>
        public static void BugEvidence(string bugId, string testName, string expected, string actual,
            IWebDriver? driver = null, params (string Key, object? Value)[] details)
        {
            var screenshot = Screenshot(driver, $"{bugId}_{testName}");
            var context = string.Join(" | ", details.Select(d => $"{d.Key}={d.Value}"));
            Logger.Warning("{BugId} confirmed in {Test}: expected [{Expected}], actual [{Actual}] | URL={Url} | {Context} | screenshot={Screenshot}",
                bugId, testName, expected, actual, driver?.Url, context, screenshot);
        }

        /// <summary>Saves a screenshot of a failed test and returns its path, or null when there is no browser.</summary>
        public static string? FailureScreenshot(IWebDriver? driver, string testName) =>
            Screenshot(driver, $"FAILED_{testName}");

        /// <summary>Saves a screenshot in TestLogs/screenshots; a failing screenshot never fails the test itself.</summary>
        private static string? Screenshot(IWebDriver? driver, string name)
        {
            if (driver == null) return null;
            try
            {
                var path = Path.Combine(LogDirectory, "screenshots", $"{name}_{DateTime.Now:HHmmss}.png");
                ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(path);
                return path;
            }
            catch (WebDriverException ex)
            {
                Logger.Debug(ex, "Taking a screenshot failed");
                return null;
            }
        }

        /// <summary>
        /// Writes to the current Console.Out. MSTest gives every test its own Console.Out,
        /// so this way the log lines show up under the test that wrote them.
        /// </summary>
        private sealed class CurrentConsoleSink(string template) : ILogEventSink
        {
            private readonly MessageTemplateTextFormatter _formatter = new(template);

            public void Emit(LogEvent logEvent) => _formatter.Format(logEvent, Console.Out);
        }
    }
}
