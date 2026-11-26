using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server.Tests.Utils
{
    /// <summary>
    /// Kleine lokale webserver die iCal/ICS-agenda's uitserveert, zodat de tests niet van
    /// internet of een echte Google/Outlook/Apple-agenda afhangen.
    /// Routes: /agenda-{n}.ics (geldig ICS), /geen-agenda (200 met HTML), al het andere geeft 404.
    /// </summary>
    public sealed class TestIcsServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly Dictionary<string, string> _calendars = new();
        private readonly string _baseUrl;
        private int _counter;

        public TestIcsServer()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            _baseUrl = $"http://localhost:{port}";
            _listener.Prefixes.Add(_baseUrl + "/");
            _listener.Start();
            _ = Task.Run(ServeAsync);
        }

        /// <summary>Geldige URL die verwijst naar een geldig ICS-bestand met één afspraak.</summary>
        public string AddCalendar(DateTime start, TimeSpan duration, string prodId = "-//Google Inc//Google Calendar 70.9054//EN")
        {
            var path = $"/agenda-{Interlocked.Increment(ref _counter)}.ics";
            _calendars[path] = IcsBuilder.Build(prodId, "Privéafspraak", start, start + duration);
            return _baseUrl + path;
        }

        /// <summary>Geldige URL die verwijst naar een geldig ICS-bestand zonder afspraken.</summary>
        public string AddEmptyCalendar()
        {
            var path = $"/agenda-{Interlocked.Increment(ref _counter)}.ics";
            _calendars[path] = IcsBuilder.BuildEmpty();
            return _baseUrl + path;
        }

        /// <summary>Geldige URL die 200 teruggeeft, maar geen agenda is.</summary>
        public string NotACalendarUrl => _baseUrl + "/geen-agenda";

        /// <summary>Geldige URL die een 404 teruggeeft.</summary>
        public string NotFoundUrl => _baseUrl + "/bestaat-niet.ics";

        private async Task ServeAsync()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext ctx;
                try { ctx = await _listener.GetContextAsync(); }
                catch { return; }

                var path = ctx.Request.Url!.AbsolutePath;
                byte[] body;

                if (_calendars.TryGetValue(path, out var ics))
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "text/calendar; charset=utf-8";
                    body = Encoding.UTF8.GetBytes(ics);
                }
                else if (path == "/geen-agenda")
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "text/html; charset=utf-8";
                    body = Encoding.UTF8.GetBytes("<html><body>Dit is geen agenda.</body></html>");
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                    body = [];
                }

                // De browser (fetch vanuit de client) mag het antwoord lezen.
                ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
                await ctx.Response.OutputStream.WriteAsync(body);
                ctx.Response.Close();
            }
        }

        public void Dispose()
        {
            _listener.Stop();
            _listener.Close();
        }
    }

    public static class IcsBuilder
    {
        private static string Utc(DateTime local) => local.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'");

        public static string Build(string prodId, string summary, DateTime start, DateTime end) =>
            string.Join("\r\n",
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                $"PRODID:{prodId}",
                "BEGIN:VEVENT",
                $"UID:{Guid.NewGuid()}@test.nl",
                $"DTSTAMP:{Utc(DateTime.Now)}",
                $"DTSTART:{Utc(start)}",
                $"DTEND:{Utc(end)}",
                $"SUMMARY:{summary}",
                "END:VEVENT",
                "END:VCALENDAR") + "\r\n";

        public static string BuildEmpty() =>
            "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Test//NL\r\nEND:VCALENDAR\r\n";
    }
}