
using Microsoft.EntityFrameworkCore;
using Server.Models;
using Server.Repositories;
using Server.Tests.TestDb;

namespace Server.Tests.Tests.Integration
{
    /// <summary>
    /// Integratietests op de echte testdatabase: de synchronisatie van externe agenda-items.
    /// Elke test start met de seed (specialisten Dr. De Vries en Dr. Bakker, geen agenda-items).
    /// </summary>
    [TestClass]
    public class CalendarEventRepositoryTests : DatabaseTestBase
    {
        private const string DeVries = "Dr. De Vries";
        private const string Bakker = "Dr. Bakker";

        // Het sync-venster van de tests
        private static readonly DateTime From = new(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime To = new(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Inside = new(2027, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        protected override Func<DbContext, Task>? GetTestDataInitializer() => TestAFSDataInitializer.SeedAsync;

        // =========================================================
        // Helpers
        // =========================================================

        private async Task<int> SpecialistIdAsync(string fullName) =>
            await DbContext.Set<Specialist>()
                .Where(s => s.User.FullName == fullName)
                .Select(s => s.Id)
                .SingleAsync();

        private static ExternalCalendarEvent Ev(int specialistId, string uid, DateTime start, string title = "Bezet") => new()
        {
            SpecialistId = specialistId,
            ExternalUid = uid,
            Title = title,
            StartUtc = start,
            EndUtc = start.AddHours(1),
            IsAllDay = false
        };

        private static Dictionary<string, ExternalCalendarEvent> Feed(params ExternalCalendarEvent[] events) =>
            events.ToDictionary(e => e.ExternalUid);

        private async Task SeedEventsAsync(params ExternalCalendarEvent[] events)
        {
            DbContext.Set<ExternalCalendarEvent>().AddRange(events);
            await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();
        }

        private async Task SeedConnectionAsync(int specialistId)
        {
            var specialist = await DbContext.Set<Specialist>()
                .SingleAsync(s => s.Id == specialistId);

            specialist.EncryptedIcalUrl = "versleuteld";

            await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();
        }

        /// <summary>Draait de sync met een eigen context, zoals de applicatie dat per request doet.</summary>
        private async Task SyncAsync(int specialistId, Dictionary<string, ExternalCalendarEvent> feed)
        {
            await using var context = TestDatabase.CreateDbContext();
            var repository = new CalendarEventRepository(context);
            var specialist = await context.Set<Specialist>()
                .SingleAsync(s => s.Id == specialistId);

            await repository.SyncEventsAsync(specialistId, From, To, feed, specialist);
        }

        private static async Task<List<ExternalCalendarEvent>> EventsOfAsync(int specialistId)
        {
            await using var context = TestDatabase.CreateDbContext();
            return await context.Set<ExternalCalendarEvent>()
                .AsNoTracking()
                .Where(e => e.SpecialistId == specialistId)
                .OrderBy(e => e.ExternalUid)
                .ToListAsync();
        }

        // =========================================================
        // SyncEventsAsync
        // =========================================================

        [TestMethod]
        public async Task SyncEventsAsync_EventNietMeerInFeed_WordtVerwijderd()
        {
            // Arrange
            var id = await SpecialistIdAsync(DeVries);
            await SeedConnectionAsync(id);
            await SeedEventsAsync(
                Ev(id, "a", Inside),
                Ev(id, "b", Inside.AddDays(1)),
                Ev(id, "c", Inside.AddDays(2)));

            // Act: "b" is uit de agenda van de specialist verdwenen
            await SyncAsync(id, Feed(Ev(id, "a", Inside), Ev(id, "c", Inside.AddDays(2))));

            // Assert
            var uids = (await EventsOfAsync(id)).Select(e => e.ExternalUid).ToArray();
            CollectionAssert.AreEqual(new[] { "a", "c" }, uids);
        }

        [TestMethod]
        public async Task SyncEventsAsync_BestaandEventGewijzigd_WordtBijgewerktZonderDuplicaat()
        {
            // Arrange
            var id = await SpecialistIdAsync(DeVries);
            await SeedConnectionAsync(id);
            await SeedEventsAsync(Ev(id, "a", Inside, "Oud"));
            var originalId = (await EventsOfAsync(id)).Single().Id;

            var moved = Ev(id, "a", Inside.AddHours(3), "Nieuw");
            moved.IsAllDay = true;

            // Act
            await SyncAsync(id, Feed(moved));

            // Assert
            var result = (await EventsOfAsync(id)).Single();
            Assert.AreEqual(originalId, result.Id, "Het bestaande item moet worden bijgewerkt, niet vervangen.");
            Assert.AreEqual("Nieuw", result.Title);
            Assert.AreEqual(Inside.AddHours(3), result.StartUtc);
            Assert.AreEqual(Inside.AddHours(4), result.EndUtc);
            Assert.IsTrue(result.IsAllDay);
        }

        [TestMethod]
        public async Task SyncEventsAsync_NieuwEvent_WordtToegevoegdNaastBestaande()
        {
            // Arrange
            var id = await SpecialistIdAsync(DeVries);
            await SeedConnectionAsync(id);
            await SeedEventsAsync(Ev(id, "a", Inside));

            // Act
            await SyncAsync(id, Feed(Ev(id, "a", Inside), Ev(id, "b", Inside.AddDays(1))));

            // Assert
            var uids = (await EventsOfAsync(id)).Select(e => e.ExternalUid).ToArray();
            CollectionAssert.AreEqual(new[] { "a", "b" }, uids);
        }

        [TestMethod]
        public async Task SyncEventsAsync_GemengdeFeed_VerwijdertUpdatetEnVoegtToeInEenKeer()
        {
            // Arrange
            var id = await SpecialistIdAsync(DeVries);
            await SeedConnectionAsync(id);
            await SeedEventsAsync(
                Ev(id, "blijft", Inside, "Oud"),
                Ev(id, "weg", Inside.AddDays(1)));

            // Act
            await SyncAsync(id, Feed(
                Ev(id, "blijft", Inside, "Bijgewerkt"),
                Ev(id, "nieuw", Inside.AddDays(2))));

            // Assert
            var result = await EventsOfAsync(id);
            CollectionAssert.AreEqual(new[] { "blijft", "nieuw" }, result.Select(e => e.ExternalUid).ToArray());
            Assert.AreEqual("Bijgewerkt", result.Single(e => e.ExternalUid == "blijft").Title);
        }

        [TestMethod]
        public async Task SyncEventsAsync_LegeFeed_VerwijdertAlleItemsInHetVenster()
        {
            // Arrange
            var id = await SpecialistIdAsync(DeVries);
            await SeedConnectionAsync(id);
            await SeedEventsAsync(Ev(id, "a", Inside), Ev(id, "b", Inside.AddDays(1)));

            // Act
            await SyncAsync(id, Feed());

            // Assert
            Assert.AreEqual(0, (await EventsOfAsync(id)).Count);
        }

        [TestMethod]
        public async Task SyncEventsAsync_EventBuitenHetVenster_BlijftBestaan()
        {
            // Arrange: één item ver na het sync-venster, die staat niet in de feed
            var id = await SpecialistIdAsync(DeVries);
            await SeedConnectionAsync(id);
            await SeedEventsAsync(
                Ev(id, "binnen", Inside),
                Ev(id, "ver-weg", To.AddDays(30)));

            // Act
            await SyncAsync(id, Feed());

            // Assert
            var uids = (await EventsOfAsync(id)).Select(e => e.ExternalUid).ToArray();
            CollectionAssert.AreEqual(new[] { "ver-weg" }, uids);
        }

        [TestMethod]
        public async Task SyncEventsAsync_AgendaVanAndereSpecialist_BlijftOnaangetast()
        {
            // Arrange
            var devries = await SpecialistIdAsync(DeVries);
            var bakker = await SpecialistIdAsync(Bakker);
            await SeedConnectionAsync(devries);
            await SeedConnectionAsync(bakker);
            await SeedEventsAsync(
                Ev(devries, "a", Inside),
                Ev(bakker, "a", Inside, "Van Bakker"));

            // Act: De Vries krijgt een lege feed, ook al heeft Bakker hetzelfde UID
            await SyncAsync(devries, Feed());

            // Assert
            Assert.AreEqual(0, (await EventsOfAsync(devries)).Count);
            var bakkerEvents = await EventsOfAsync(bakker);
            Assert.AreEqual(1, bakkerEvents.Count);
            Assert.AreEqual("Van Bakker", bakkerEvents[0].Title);
        }

        [TestMethod]
        public async Task SyncEventsAsync_ZetLastSyncedUtcOpDeKoppeling()
        {
            // Arrange
            var id = await SpecialistIdAsync(DeVries);
            await SeedConnectionAsync(id);
            var before = DateTime.UtcNow.AddSeconds(-1);

            // Act
            await SyncAsync(id, Feed(Ev(id, "a", Inside)));

            // Assert
            await using var verify = TestDatabase.CreateDbContext();
            var specialist = await verify.Set<Specialist>()
                .AsNoTracking()
                .SingleAsync(s => s.Id == id);

            Assert.IsNotNull(specialist.LastSyncedUtc);
            Assert.IsTrue(specialist.LastSyncedUtc >= before, "LastSyncedUtc moet de tijd van deze sync zijn.");
        }

        [TestMethod]
        public async Task SyncEventsAsync_TweeKeerMetDezelfdeFeed_GeeftGeenDuplicaten()
        {
            // Arrange
            var id = await SpecialistIdAsync(DeVries);
            await SeedConnectionAsync(id);
            var feed = Feed(Ev(id, "a", Inside), Ev(id, "b", Inside.AddDays(1)));

            // Act
            await SyncAsync(id, feed);
            await SyncAsync(id, Feed(Ev(id, "a", Inside), Ev(id, "b", Inside.AddDays(1))));

            // Assert
            Assert.AreEqual(2, (await EventsOfAsync(id)).Count);
        }

        // =========================================================
        // GetBySpecialistAndRangeAsync (grenswaarden)
        // =========================================================

        [TestMethod]
        public async Task GetBySpecialistAndRangeAsync_GrenzenZijnInclusief_ItemsDaarbuitenNiet()
        {
            // Arrange
            var id = await SpecialistIdAsync(DeVries);
            await SeedEventsAsync(
                Ev(id, "voor", From.AddSeconds(-1)),
                Ev(id, "begin", From),
                Ev(id, "einde", To),
                Ev(id, "na", To.AddSeconds(1)));

            await using var context = TestDatabase.CreateDbContext();
            var repository = new CalendarEventRepository(context);

            // Act
            var result = await repository.GetBySpecialistAndRangeAsync(id, From, To);

            // Assert
            var uids = result.Select(e => e.ExternalUid).OrderBy(u => u).ToArray();
            CollectionAssert.AreEqual(new[] { "begin", "einde" }, uids);
        }
    }
}
