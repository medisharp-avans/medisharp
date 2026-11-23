using Server.Tests.Utils;

namespace Server.Tests
{
    /// <summary>Runs once after the last test: stops the apps the tests started and writes the last log lines.</summary>
    [TestClass]
    public class TestRun
    {
        /// <summary>Stops the apps and flushes the log files.</summary>
        [AssemblyCleanup]
        public static void Cleanup()
        {
            AppServer.Stop();
            (TestLog.Logger as IDisposable)?.Dispose();
        }
    }
}
