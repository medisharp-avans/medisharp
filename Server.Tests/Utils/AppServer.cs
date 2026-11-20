using System.Collections.Concurrent;
using System.Diagnostics;

namespace Server.Tests.Utils
{
    /// <summary>
    /// Starts the API and the Vue client for the end-to-end tests and stops them after the test run,
    /// so the tests also work with a plain 'dotnet test'. An app that is already running
    /// (for example started from Visual Studio) is used as it is and is not stopped afterwards.
    /// </summary>
    public static class AppServer
    {
        private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(90);
        private static readonly List<Process> Started = [];
        private static bool _isStarted;

#if DEBUG
        private const string Configuration = "Debug";
#else
        private const string Configuration = "Release";
#endif

        public static void EnsureStarted()
        {
            if (_isStarted) return;

            var root = FindRepositoryRoot();

            // Same as the 'Tests' launch profile in Visual Studio: the URL of ApiUrl and the test database.
            // '--no-build' because the API is already built together with this test project.
            Start("API", TestSettings.ApiUrl, Path.Combine(root, "Server"),
                "dotnet", ["run", "--no-build", "-c", Configuration, "--launch-profile", "Tests"]);

            // Vite is started with node directly (like 'npm run dev'), because npm.cmd cannot be started reliably
            // without a shell on Windows. Run 'npm install' in Client once before the first test run.
            var port = new Uri(TestSettings.ClientUrl).Port.ToString();
            Start("Client", TestSettings.ClientUrl, Path.Combine(root, "Client"),
                "node", [Path.Combine("node_modules", "vite", "bin", "vite.js"), "--port", port, "--strictPort"]);

            _isStarted = true;
        }

        /// <summary>Stops only the apps that this class started.</summary>
        public static void Stop()
        {
            foreach (var process in Started)
            {
                // 'dotnet run' and 'npm' start the app as a child process, so the whole process tree is stopped
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                process.Dispose();
            }
            Started.Clear();
            _isStarted = false;
        }

        private static void Start(string name, string url, string workingDirectory, string fileName, string[] arguments)
        {
            if (IsRunning(url))
            {
                TestLog.Logger.Information("{App} is already running on {Url}, using that one", name, url);
                return;
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
            // The output must be read, otherwise the app blocks once the output buffer is full
            process.OutputDataReceived += (_, e) => KeepLast(lastOutput, e.Data);
            process.ErrorDataReceived += (_, e) => KeepLast(lastOutput, e.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            Started.Add(process);

            var stopwatch = Stopwatch.StartNew();
            while (!IsRunning(url))
            {
                if (process.HasExited || stopwatch.Elapsed > StartTimeout)
                {
                    Stop();
                    throw new InvalidOperationException(
                        $"{name} did not start on {url}. Last output:{Environment.NewLine}{string.Join(Environment.NewLine, lastOutput)}");
                }
                Thread.Sleep(500);
            }
            TestLog.Logger.Information("{App} started in {Seconds:0} s", name, stopwatch.Elapsed.TotalSeconds);
        }

        /// <summary>An app is running when it answers on its URL, with any status code. The dev certificate is not checked.</summary>
        private static bool IsRunning(string url)
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };
            try
            {
                client.GetAsync(url).GetAwaiter().GetResult();
                return true;
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                return false;
            }
        }

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

        private static void KeepLast(ConcurrentQueue<string> output, string? line)
        {
            if (line == null) return;
            output.Enqueue(line);
            while (output.Count > 30)
                output.TryDequeue(out _);
        }
    }
}
