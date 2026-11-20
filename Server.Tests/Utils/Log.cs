using OpenQA.Selenium;
using Serilog;

namespace Server.Tests.Utils
{
    public static class TestLog
    {
        public static ILogger Logger { get; }
        public static string LogDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "TestLogs");

        static TestLog()
        {
            Directory.CreateDirectory(Path.Combine(LogDirectory, "screenshots"));

            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(Path.Combine(LogDirectory, "TestExecutionLogs-.txt"), rollingInterval: RollingInterval.Day)

                .WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("BugId"))
                    .WriteTo.File(Path.Combine(LogDirectory, "BugEvidence-.txt"), rollingInterval: RollingInterval.Day))
                .CreateLogger();
        }

        /// <summary>Legt vast dat een bekende bug zich voordoet: verwacht vs. werkelijk, context en screenshot.</summary>
        public static void BugEvidence(string bugId, string testName, string expected, string actual,
            IWebDriver? driver = null, params (string Key, object? Value)[] details)
        {
            string? screenshot = null;
            if (driver != null)
            {
                try
                {
                    screenshot = Path.Combine(LogDirectory, "screenshots", $"{bugId}_{testName}_{DateTime.Now:HHmmss}.png");
                    ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(screenshot);
                }
                catch (Exception ex)
                {
                    Logger.Debug(ex, "Screenshot maken mislukt");
                    screenshot = null;
                }
            }

            var context = string.Join(" | ", details.Select(d => $"{d.Key}={d.Value}"));
            Logger.Warning("{BugId} bevestigd in {Test}: verwacht [{Expected}], werkelijk [{Actual}] | URL={Url} | {Context} | screenshot={Screenshot}",
                bugId, testName, expected, actual, driver?.Url, context, screenshot);
        }

        public static void Step(string message) => Logger.Debug("    · {Message}", message);

        public static string? FailureScreenshot(IWebDriver? driver, string testName)
        {
            if (driver == null) return null;
            try
            {
                var path = Path.Combine(LogDirectory, "screenshots", $"FAILED_{testName}_{DateTime.Now:HHmmss}.png");
                ((ITakesScreenshot)driver).GetScreenshot().SaveAsFile(path);
                return path;
            }
            catch { return null; }
        }
    }
}