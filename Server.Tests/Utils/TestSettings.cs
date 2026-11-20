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
        private static readonly IConfiguration Config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.test.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        /// <summary>The Vue client, opened by the browser in end-to-end tests.</summary>
        public static string ClientUrl { get; } = Required("ClientUrl").TrimEnd('/');

        /// <summary>The API, started with the 'Tests' launch profile.</summary>
        public static string ApiUrl { get; } = Required("ApiUrl").TrimEnd('/');

        public static string ConnectionString { get; } = Required("ConnectionStrings:DefaultConnection");

        /// <summary>How long waits for elements and URLs take before they fail.</summary>
        public static TimeSpan Timeout { get; } =
            TimeSpan.FromSeconds(double.Parse(Config["TimeoutSeconds"] ?? "4", CultureInfo.InvariantCulture));

        /// <summary>Runs Chrome without a window, for example on a build server.</summary>
        public static bool Headless { get; } = bool.Parse(Config["Headless"] ?? "false");

        private static string Required(string key) =>
            Config[key] ?? throw new InvalidOperationException($"Setting '{key}' not found in appsettings.test.json.");
    }
}
