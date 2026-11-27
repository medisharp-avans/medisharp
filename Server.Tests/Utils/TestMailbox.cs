using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Server.Tests.Utils
{
    /// <summary>
    /// Reads the emails that the API writes to its pickup folder instead of sending them
    /// (SMTP__PickupDirectory in the 'Tests' launch profile), for example to get the login code.
    /// </summary>
    public static class TestMailbox
    {
        /// <summary>Must match SMTP__PickupDirectory of the 'Tests' launch profile (relative to Server).</summary>
        public static string Directory { get; } = Path.Combine(AppServer.RepositoryRoot, "Server", "bin", "test-mails");

        private static readonly Regex _code = new(@"verificatiecode is: (\d{6})", RegexOptions.Compiled);

        /// <summary>The emails that are in the folder now. Pass it to <see cref="WaitForLoginCode"/> to skip older emails.</summary>
        public static HashSet<string> Snapshot() =>
            System.IO.Directory.Exists(Directory) ? [.. System.IO.Directory.GetFiles(Directory, "*.eml")] : [];

        /// <summary>
        /// Waits for a new email to this address with a login code, deletes it and returns the code.
        /// </summary>
        public static string WaitForLoginCode(string email, HashSet<string> before)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < TestSettings.Timeout)
            {
                foreach (var file in Snapshot().Except(before))
                {
                    string text;
                    try
                    {
                        // Undo the quoted-printable soft line breaks
                        text = File.ReadAllText(file).Replace("=\r\n", "");
                    }
                    catch (IOException)
                    {
                        continue; // Still being written; try again on the next round
                    }

                    if (!text.Contains($"To: {email}", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var match = _code.Match(text);
                    if (!match.Success)
                        continue;

                    File.Delete(file);
                    TestLog.Step($"Login code for {email} read from the test mailbox");
                    return match.Groups[1].Value;
                }
                Thread.Sleep(100);
            }

            throw new TimeoutException(
                $"No login code for {email} in {Directory} within {TestSettings.Timeout.TotalSeconds} s. " +
                "Is the API started with the 'Tests' launch profile?");
        }
    }
}
