using System.Collections.Concurrent;
using System.Diagnostics;

namespace Server.Tests.Utils
{
    /// <summary>
    /// Starts the API and the Vue client for the end-to-end tests and stops them after the test run,
    /// so the tests also work with a plain 'dotnet test'. This requires dedicated test ports.
    /// </summary>
    public static class AppServer
    {
        private static readonly TimeSpan _startTimeout = TimeSpan.FromSeconds(90);
        private static readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(500);
        private static readonly List<Process> _started = [];

        // Only used to check whether an app answers. The development certificate is accepted without checking it.
        private static readonly HttpClient _httpClient = new(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        })
        { Timeout = TimeSpan.FromSeconds(2) };

        private static bool _isStarted;

#if DEBUG
        private const string Configuration = "Debug";
#else
        private const string Configuration = "Release";
#endif

        /// <summary>Starts the API and the client once for the whole test run; later calls do nothing.</summary>
        public static async Task EnsureStartedAsync()
        {
            if (_isStarted) return;

            var root = RepositoryRoot;

            await StartAsync("API", TestSettings.ApiUrl, Path.Combine(root, "Server"),
                "dotnet", ["run", "--no-build", "-c", Configuration, "--no-launch-profile"],
                new()
                {
                    ["ASPNETCORE_ENVIRONMENT"] = "Development",
                    ["ASPNETCORE_URLS"] = TestSettings.ApiUrl,
                    ["ConnectionStrings__DefaultConnection"] = TestSettings.ConnectionString,
                    ["ConnectionStrings__ReferralConnection"] = TestSettings.ReferralConnectionString,
                    ["Database__SeedDemoData"] = "false",
                    ["SMTP__PickupDirectory"] = "bin/test-mails"
                });

            // Vite is started with node directly (like 'npm run dev'), because npm.cmd cannot be started reliably
            // without a shell on Windows. Run 'npm install' in Client once before the first test run.
            var port = new Uri(TestSettings.ClientUrl).Port.ToString();
            await StartAsync("Client", TestSettings.ClientUrl, Path.Combine(root, "Client"),
                "node", [Path.Combine("node_modules", "vite", "bin", "vite.js"), "--mode", "test", "--port", port, "--strictPort"],
                // The client must call the test API, not the development API it uses by default
                new() { ["VITE_API_PROXY_TARGET"] = TestSettings.ApiUrl, ["VITE_API_URL"] = string.Empty });

            _isStarted = true;
        }

        /// <summary>Stops only the apps that this class started.</summary>
        public static void Stop()
        {
            foreach (var process in _started)
            {
                // 'dotnet run' starts the app as a child process, so the whole process tree is stopped
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                process.Dispose();
            }
            _started.Clear();
            _isStarted = false;
        }

        /// <summary>
        /// Starts one app and waits until it answers on its URL.
        /// Fails with the last output of the app when it stops or does not answer within the start timeout.
        /// </summary>
        private static async Task StartAsync(string name, string url, string workingDirectory, string fileName, string[] arguments,
            Dictionary<string, string>? environment = null)
        {
            if (await IsRunningAsync(url))
            {
                throw new InvalidOperationException($"The {name} test port is already in use at {url}. Use a free test port.");
            }

            TestLog.Logger.Information("Starting {App} on {Url}...", name, url);
            var lastOutput = new ConcurrentQueue<string>();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo(fileName, arguments)
                {
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                },
            };
            foreach (var (key, value) in environment ?? [])
                process.StartInfo.Environment[key] = value;
            // The output must be read, otherwise the app blocks once the output buffer is full
            process.OutputDataReceived += (_, e) => KeepLast(lastOutput, e.Data);
            process.ErrorDataReceived += (_, e) => KeepLast(lastOutput, e.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            _started.Add(process);

            var stopwatch = Stopwatch.StartNew();
            while (!await IsRunningAsync(url))
            {
                if (process.HasExited || stopwatch.Elapsed > _startTimeout)
                {
                    Stop();
                    throw new InvalidOperationException(
                        $"{name} did not start on {url}. Last output:{Environment.NewLine}{string.Join(Environment.NewLine, lastOutput)}");
                }
                await Task.Delay(_pollInterval);
            }
            TestLog.Logger.Information("{App} started in {Seconds:0} s", name, stopwatch.Elapsed.TotalSeconds);
        }

        /// <summary>An app is running when it answers on its URL, with any status code.</summary>
        private static async Task<bool> IsRunningAsync(string url)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url);
                return true;
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                return false;
            }
        }

        /// <summary>The folder with Server and Client.</summary>
        public static string RepositoryRoot { get; } = FindRepositoryRoot();

        /// <summary>The folder with Server and Client, found from the build output of this test project.</summary>
        private static string FindRepositoryRoot()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Server")) && Directory.Exists(Path.Combine(dir.FullName, "Client")))
                    return dir.FullName;
            }
            throw new InvalidOperationException($"No folder with 'Server' and 'Client' found above {AppContext.BaseDirectory}.");
        }

        /// <summary>Keeps only the last 30 lines an app wrote, to show them when the app does not start.</summary>
        private static void KeepLast(ConcurrentQueue<string> output, string? line)
        {
            if (line == null) return;
            output.Enqueue(line);
            while (output.Count > 30)
                output.TryDequeue(out _);
        }
    }
}
