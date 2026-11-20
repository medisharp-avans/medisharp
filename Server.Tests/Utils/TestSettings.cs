using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace Server.Tests.Utils
{
    /// <summary>
    /// The settings of appsettings.test.json, read once for the whole test run.
    /// A missing setting fails with a clear message instead of silently falling back to a default.
    /// </summary>
    public static class TestSettings
    {
        // Environment variables override the JSON, for example ConnectionStrings__DefaultConnection in the CI
        private static readonly IConfiguration _config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.test.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        /// <summary>The Vue client, opened by the browser in end-to-end tests.</summary>
        public static string ClientUrl { get; } = Required("ClientUrl").TrimEnd('/');

        /// <summary>The API, started with the 'Tests' launch profile.</summary>
        public static string ApiUrl { get; } = Required("ApiUrl").TrimEnd('/');

        /// <summary>The test database. Its name must end with "Test".</summary>
        public static string ConnectionString { get; } = Required("ConnectionStrings:DefaultConnection");

        /// <summary>How long waits for elements and URLs take before they fail.</summary>
        public static TimeSpan Timeout { get; } =
            TimeSpan.FromSeconds(double.Parse(_config["TimeoutSeconds"] ?? "4", CultureInfo.InvariantCulture));

        /// <summary>Runs Chrome without a window, for example on a build server.</summary>
        public static bool Headless { get; } = bool.Parse(_config["Headless"] ?? "false");

        /// <summary>Reads a setting that must exist.</summary>
        private static string Required(string key) =>
            _config[key] ?? throw new InvalidOperationException($"Setting '{key}' not found in appsettings.test.json.");
    }
}
