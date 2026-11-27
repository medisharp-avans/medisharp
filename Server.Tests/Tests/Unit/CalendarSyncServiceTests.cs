using Microsoft.AspNetCore.DataProtection;
using Moq;
using Server.Models;
using Server.Repositories.InterFaces;
using Server.Services;
using System.Globalization;
using System.Net;

namespace Server.Tests.Unit
{
    /// <summary>Stuurt elk HTTP-verzoek naar een vaste reactie en onthoudt welke URL's zijn opgevraagd.</summary>
    internal sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return Task.FromResult(Responder(request));
        }
    }

    /// <summary>
    /// Unit-tests van CalendarSyncService: repositories en HTTP zijn gemockt,
    /// de versleuteling is de echte (in-memory) DataProtection.
    /// </summary>
    [TestClass]
    public class CalendarSyncServiceTests
    {
        private const int SpecialistId = 7;
        private const string FeedUrl = "https://example.com/feed.ics";

        private Mock<ICalendarConnectionRepository> _connections = null!;
        private Mock<ICalendarEventRepository> _events = null!;
        private Mock<IHttpClientFactory> _httpFactory = null!;
        private StubHttpMessageHandler _http = null!;
        private IDataProtectionProvider _provider = null!;
        private IDataProtector _protector = null!;
        private CalendarSyncService _service = null!;

        // Wat de service aan de repositories heeft doorgegeven
        private string? _savedEncryptedUrl;
        private Dictionary<string, ExternalCalendarEvent>? _syncedEvents;
        private DateTime _syncedFrom;
        private DateTime _syncedTo;

        [TestInitialize]
        public void Setup()
        {
            _connections = new Mock<ICalendarConnectionRepository>();
            _events = new Mock<ICalendarEventRepository>();
            _httpFactory = new Mock<IHttpClientFactory>();
            _http = new StubHttpMessageHandler();
            _provider = new EphemeralDataProtectionProvider();
            _protector = _provider.CreateProtector("UserIcalUrls");

            _savedEncryptedUrl = null;
            _syncedEvents = null;

            _httpFactory
                .Setup(f => f.CreateClient(It.IsAny<string>()))
                .Returns(() => new HttpClient(_http, disposeHandler: false));

            _connections
                .Setup(c => c.GetConnectionUser(It.IsAny<int>()))
                .ReturnsAsync((UserCalendarConnection?)null);

            _connections
                .Setup(c => c.UpdateUserCalendarConnection(It.IsAny<UserCalendarConnection?>(), It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((UserCalendarConnection? existing, int id, string url) =>
                {
                    _savedEncryptedUrl = url;
                    return existing ?? new UserCalendarConnection { SpecialistId = id, EncryptedIcalUrl = url };
                });

            _events
                .Setup(e => e.SyncEventsAsync(
                    It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                    It.IsAny<Dictionary<string, ExternalCalendarEvent>>(), It.IsAny<UserCalendarConnection>()))
                .Callback<int, DateTime, DateTime, Dictionary<string, ExternalCalendarEvent>, UserCalendarConnection>(
                    (_, from, to, events, _) =>
                    {
                        _syncedFrom = from;
                        _syncedTo = to;
                        _syncedEvents = events;
                    })
                .Returns(Task.CompletedTask);

            _service = new CalendarSyncService(_httpFactory.Object, _provider, _connections.Object, _events.Object);
        }

        // =========================================================
        // Helpers
        // =========================================================

        private static string Utc(DateTime d) => d.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

        private static string VEvent(string uid, DateTime startUtc, string? summary = "Privé", string? rrule = null) =>
            string.Join("\r\n", new[]
            {
                "BEGIN:VEVENT",
                $"UID:{uid}",
                $"DTSTAMP:{Utc(DateTime.UtcNow)}",
                $"DTSTART:{Utc(startUtc)}",
                $"DTEND:{Utc(startUtc.AddHours(1))}",
                summary is null ? null : $"SUMMARY:{summary}",
                rrule is null ? null : $"RRULE:{rrule}",
                "END:VEVENT"
            }.Where(l => l is not null)) + "\r\n";

        private static string Ics(params string[] events) =>
            "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Test//NL\r\n" + string.Concat(events) + "END:VCALENDAR\r\n";

        private void ServeIcs(string ics) =>
            _http.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ics) };

        private UserCalendarConnection ConnectionTo(string url) => new()
        {
            SpecialistId = SpecialistId,
            EncryptedIcalUrl = _protector.Protect(url)
        };

        private static DateTime InTwoDays => DateTime.UtcNow.Date.AddDays(2).AddHours(9);

        // =========================================================
        // SaveAndSyncUrlAsync – validatie
        // =========================================================

        [TestMethod]
        [DataRow("ftp://example.com/agenda.ics", DisplayName = "ftp")]
        [DataRow("file:///c:/agenda.ics", DisplayName = "file")]
        [DataRow("geen-link", DisplayName = "geen URL")]
        [DataRow("", DisplayName = "leeg")]
        public async Task SaveAndSyncUrlAsync_OngeldigeUrl_GooitArgumentExceptionEnSlaatNietsOp(string url)
        {
            // Act
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => _service.SaveAndSyncUrlAsync(SpecialistId, url));

            // Assert
            Assert.AreEqual(0, _http.Requests.Count, "Er mag niets opgehaald worden bij een ongeldige URL.");
            _connections.Verify(c => c.UpdateUserCalendarConnection(It.IsAny<UserCalendarConnection?>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _events.Verify(e => e.SyncEventsAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, ExternalCalendarEvent>>(), It.IsAny<UserCalendarConnection>()), Times.Never);
        }

        [TestMethod]
        public async Task SaveAndSyncUrlAsync_ServerGeeft404_GooitArgumentExceptionEnSlaatNietsOp()
        {
            // Arrange
            _http.Responder = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

            // Act
            var exception = await Assert.ThrowsExactlyAsync<ArgumentException>(
                () => _service.SaveAndSyncUrlAsync(SpecialistId, FeedUrl));

            // Assert
            Assert.IsInstanceOfType<HttpRequestException>(exception.InnerException);
            _connections.Verify(c => c.UpdateUserCalendarConnection(It.IsAny<UserCalendarConnection?>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task SaveAndSyncUrlAsync_ResponseIsGeenAgenda_GooitArgumentExceptionEnSlaatNietsOp()
        {
            // Arrange
            _http.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>Geen agenda</html>") };

            // Act
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => _service.SaveAndSyncUrlAsync(SpecialistId, FeedUrl));

            // Assert
            _connections.Verify(c => c.UpdateUserCalendarConnection(It.IsAny<UserCalendarConnection?>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
            _events.Verify(e => e.SyncEventsAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, ExternalCalendarEvent>>(), It.IsAny<UserCalendarConnection>()), Times.Never);
        }

        // =========================================================
        // SaveAndSyncUrlAsync – opslaan en synchroniseren
        // =========================================================

        [TestMethod]
        public async Task SaveAndSyncUrlAsync_WebcalUrlMetSpaties_WordtHttpsEnVersleuteldOpgeslagen()
        {
            // Arrange
            ServeIcs(Ics(VEvent("abc", InTwoDays)));

            // Act
            await _service.SaveAndSyncUrlAsync(SpecialistId, "  webcal://example.com/feed.ics  ");

            // Assert
            Assert.AreEqual(new Uri("https://example.com/feed.ics"), _http.Requests.Single());

            Assert.IsNotNull(_savedEncryptedUrl);
            Assert.AreNotEqual(FeedUrl, _savedEncryptedUrl, "De URL mag niet leesbaar worden opgeslagen.");
            Assert.AreEqual(FeedUrl, _protector.Unprotect(_savedEncryptedUrl));
        }

        [TestMethod]
        public async Task SaveAndSyncUrlAsync_BestaandeKoppeling_WordtHergebruiktInPlaatsVanEenNieuweAan()
        {
            // Arrange
            ServeIcs(Ics(VEvent("abc", InTwoDays)));
            var existing = ConnectionTo("https://oud.example.com/feed.ics");
            _connections.Setup(c => c.GetConnectionUser(SpecialistId)).ReturnsAsync(existing);

            // Act
            await _service.SaveAndSyncUrlAsync(SpecialistId, FeedUrl);

            // Assert
            _connections.Verify(c => c.UpdateUserCalendarConnection(existing, SpecialistId, It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task SaveAndSyncUrlAsync_GeldigeAgenda_SynchroniseertMetDeAlGedownloadeInhoud()
        {
            // Arrange
            ServeIcs(Ics(VEvent("abc", InTwoDays)));

            // Act
            await _service.SaveAndSyncUrlAsync(SpecialistId, FeedUrl);

            // Assert
            Assert.AreEqual(1, _http.Requests.Count, "De agenda moet maar één keer opgehaald worden.");
            Assert.IsNotNull(_syncedEvents);
            Assert.AreEqual(1, _syncedEvents.Count);
        }

        // =========================================================
        // SyncUserEventsAsync – vertalen van de agenda
        // =========================================================

        [TestMethod]
        public async Task SyncUserEventsAsync_EventInVenster_WordtDoorgegevenMetJuisteVelden()
        {
            // Arrange
            var start = InTwoDays;
            var ics = Ics(VEvent("abc-1", start, summary: "Tandarts"));

            // Act
            await _service.SyncUserEventsAsync(ConnectionTo(FeedUrl), ics);

            // Assert
            Assert.IsNotNull(_syncedEvents);
            var item = _syncedEvents.Values.Single();
            Assert.IsTrue(item.ExternalUid.StartsWith("abc-1_"), item.ExternalUid);
            Assert.AreEqual(SpecialistId, item.SpecialistId);
            Assert.AreEqual("Tandarts", item.Title);
            Assert.AreEqual(start, item.StartUtc);
            Assert.AreEqual(start.AddHours(1), item.EndUtc);
            Assert.IsFalse(item.IsAllDay);
        }

        [TestMethod]
        public async Task SyncUserEventsAsync_EventZonderSamenvatting_KrijgtTitelBezet()
        {
            // Arrange
            var ics = Ics(VEvent("abc-1", InTwoDays, summary: null));

            // Act
            await _service.SyncUserEventsAsync(ConnectionTo(FeedUrl), ics);

            // Assert
            Assert.AreEqual("Bezet", _syncedEvents!.Values.Single().Title);
        }

        [TestMethod]
        public async Task SyncUserEventsAsync_WekelijksHerhalendEvent_GeeftEenUniekItemPerHerhaling()
        {
            // Arrange
            var start = InTwoDays;
            var ics = Ics(VEvent("wekelijks", start, rrule: "FREQ=WEEKLY;COUNT=3"));

            // Act
            await _service.SyncUserEventsAsync(ConnectionTo(FeedUrl), ics);

            // Assert
            Assert.AreEqual(3, _syncedEvents!.Count);
            var starts = _syncedEvents.Values.Select(e => e.StartUtc).OrderBy(s => s).ToArray();
            CollectionAssert.AreEqual(new[] { start, start.AddDays(7), start.AddDays(14) }, starts);
        }

        [TestMethod]
        public async Task SyncUserEventsAsync_EventsBuitenHetVenster_WordenGenegeerd()
        {
            // Arrange: ruim een maand geleden, ruim zes maanden vooruit, en één binnen het venster
            var ics = Ics(
                VEvent("oud", DateTime.UtcNow.AddMonths(-3)),
                VEvent("binnen", InTwoDays),
                VEvent("te-ver", DateTime.UtcNow.AddMonths(8)));

            // Act
            await _service.SyncUserEventsAsync(ConnectionTo(FeedUrl), ics);

            // Assert
            var item = _syncedEvents!.Values.Single();
            Assert.IsTrue(item.ExternalUid.StartsWith("binnen_"), item.ExternalUid);
        }

        [TestMethod]
        public async Task SyncUserEventsAsync_HeleDagEvent_IsMarkeerdAlsHeleDag()
        {
            // Arrange
            var day = InTwoDays.Date;
            var ics = Ics(string.Join("\r\n",
                "BEGIN:VEVENT",
                "UID:vakantie",
                $"DTSTAMP:{Utc(DateTime.UtcNow)}",
                $"DTSTART;VALUE=DATE:{day:yyyyMMdd}",
                $"DTEND;VALUE=DATE:{day.AddDays(1):yyyyMMdd}",
                "SUMMARY:Vakantie",
                "END:VEVENT") + "\r\n");

            // Act
            await _service.SyncUserEventsAsync(ConnectionTo(FeedUrl), ics);

            // Assert
            Assert.IsTrue(_syncedEvents!.Values.Single().IsAllDay);
        }

        [TestMethod]
        public async Task SyncUserEventsAsync_LegeAgenda_GeeftLegeFeedDoorZodatOudeItemsWordenVerwijderd()
        {
            // Act
            await _service.SyncUserEventsAsync(ConnectionTo(FeedUrl), Ics());

            // Assert
            Assert.IsNotNull(_syncedEvents, "Ook een lege agenda moet gesynchroniseerd worden.");
            Assert.AreEqual(0, _syncedEvents.Count);
        }

        [TestMethod]
        public async Task SyncUserEventsAsync_GebruiktVensterVanEenMaandTerugTotZesMaandVooruit()
        {
            // Act
            await _service.SyncUserEventsAsync(ConnectionTo(FeedUrl), Ics());

            // Assert
            var expectedFrom = DateTime.UtcNow.AddMonths(-1);
            var expectedTo = DateTime.UtcNow.AddMonths(6);
            Assert.IsTrue(Math.Abs((_syncedFrom - expectedFrom).TotalMinutes) < 1, _syncedFrom.ToString("O"));
            Assert.IsTrue(Math.Abs((_syncedTo - expectedTo).TotalMinutes) < 1, _syncedTo.ToString("O"));
        }

        // =========================================================
        // SyncUserEventsAsync – ophalen van de agenda
        // =========================================================

        [TestMethod]
        public async Task SyncUserEventsAsync_ZonderVoorgeladenAgenda_HaaltDeOntsleuteldeUrlOp()
        {
            // Arrange
            ServeIcs(Ics(VEvent("abc", InTwoDays)));

            // Act
            await _service.SyncUserEventsAsync(ConnectionTo(FeedUrl));

            // Assert
            Assert.AreEqual(new Uri(FeedUrl), _http.Requests.Single());
            Assert.AreEqual(1, _syncedEvents!.Count);
        }

        [TestMethod]
        public async Task SyncUserEventsAsync_MetVoorgeladenAgenda_DoetGeenHttpVerzoek()
        {
            // Act
            await _service.SyncUserEventsAsync(ConnectionTo(FeedUrl), Ics(VEvent("abc", InTwoDays)));

            // Assert
            Assert.AreEqual(0, _http.Requests.Count);
        }

        [TestMethod]
        public async Task SyncUserEventsAsync_AgendaNietBereikbaar_GooitFoutEnRaaktDeDatabaseNiet()
        {
            // Arrange: een tijdelijk offline agenda mag de bestaande items niet wissen
            _http.Responder = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

            // Act
            await Assert.ThrowsExactlyAsync<HttpRequestException>(
                () => _service.SyncUserEventsAsync(ConnectionTo(FeedUrl)));

            // Assert
            _events.Verify(e => e.SyncEventsAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Dictionary<string, ExternalCalendarEvent>>(), It.IsAny<UserCalendarConnection>()), Times.Never);
        }
    }
}