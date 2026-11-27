
using Microsoft.EntityFrameworkCore;
using Server.Models;
using Server.Repositories;
using Server.Tests.TestDb;

namespace Server.Tests.Integration
{
    /// <summary>Integratietests op de echte testdatabase: koppelen en volledig ontkoppelen van een agenda.</summary>
    [TestClass]
    public class CalendarConnectionRepositoryTests : DatabaseTestBase
    {
        private const string DeVries = "Dr. De Vries";
        private const string Bakker = "Dr. Bakker";

        private static readonly DateTime Start = new(2027, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        protected override Func<DbContext, Task>? GetTestDataInitializer() => TestAFSDataInitializer.SeedAsync;

        // =========================================================
        // Helpers
        // =========================================================

        private async Task<int> SpecialistIdAsync(string fullName) =>
            await DbContext.Set<Specialist>()
                .Where(s => s.User.FullName == fullName)
                .Select(s => s.Id)
                .SingleAsync();

        private async Task SeedAsync(int specialistId, string encryptedUrl, int eventCount)
        {
            var specialist = await DbContext.Set<Specialist>()
                .SingleAsync(s => s.Id == specialistId);

            specialist.EncryptedIcalUrl = encryptedUrl;

            for (var i = 0; i < eventCount; i++)
            {
                DbContext.Set<ExternalCalendarEvent>().Add(new ExternalCalendarEvent
                {
                    SpecialistId = specialistId,
                    ExternalUid = $"uid-{specialistId}-{i}",
                    Title = "Bezet",
                    StartUtc = Start.AddDays(i),
                    EndUtc = Start.AddDays(i).AddHours(1)
                });
            }

            await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();
        }

        private static async Task<(int Connections, int Events)> CountsAsync(int specialistId)
        {
            await using var verify = TestDatabase.CreateDbContext();
            return (
                await verify.Set<Specialist>().CountAsync(s =>
                    s.Id == specialistId && s.EncryptedIcalUrl != null),
                await verify.Set<ExternalCalendarEvent>().CountAsync(e => e.SpecialistId == specialistId));
        }

        // =========================================================
        // UpdateUserCalendarConnection
        // =========================================================

        [TestMethod]
        public async Task UpdateUserCalendarConnection_GeenBestaandeKoppeling_MaaktNieuweAan()
        {
            // Arrange
            var id = await SpecialistIdAsync(DeVries);
            await using var context = TestDatabase.CreateDbContext();
            var repository = new CalendarConnectionRepository(context);
            var specialist = await repository.GetConnectionUser(id);

            Assert.IsNotNull(specialist);

            // Act
            var result = await repository.UpdateUserCalendarConnection(specialist, "versleuteld-1");

            // Assert
            Assert.AreEqual(id, result.Id);
            Assert.AreEqual("versleuteld-1", result.EncryptedIcalUrl);
            Assert.AreEqual(1, (await CountsAsync(id)).Connections);
        }

        [TestMethod]
        public async Task UpdateUserCalendarConnection_BestaandeKoppeling_UpdatetDeUrlZonderDuplicaat()
        {
            // Arrange
            var id = await SpecialistIdAsync(DeVries);
            await SeedAsync(id, "oud", eventCount: 0);

            await using var context = TestDatabase.CreateDbContext();
            var repository = new CalendarConnectionRepository(context);
            var existing = await repository.GetConnectionUser(id);
            Assert.IsNotNull(existing);

            // Act
            var result = await repository.UpdateUserCalendarConnection(existing, "nieuw");

            // Assert
            Assert.AreEqual(existing.Id, result.Id);
            await using var verify = TestDatabase.CreateDbContext();
            var specialist = await verify.Set<Specialist>()
                .AsNoTracking()
                .SingleAsync(s => s.Id == id);

            Assert.AreEqual("nieuw", specialist.EncryptedIcalUrl);
            Assert.AreEqual(1, (await CountsAsync(id)).Connections);
        }

        // =========================================================
        // DisconnectAsync
        // =========================================================

        [TestMethod]
        public async Task DisconnectAsync_VerwijdertKoppelingEnAlleEventsVanDeSpecialist()
        {
            // Arrange
            var id = await SpecialistIdAsync(DeVries);
            await SeedAsync(id, "versleuteld", eventCount: 3);
            Assert.AreEqual((1, 3), await CountsAsync(id));

            await using var context = TestDatabase.CreateDbContext();
            var repository = new CalendarConnectionRepository(context);

            // Act
            await repository.DisconnectAsync(id);

            // Assert
            Assert.AreEqual((0, 0), await CountsAsync(id));

            await using var verify = TestDatabase.CreateDbContext();
            var specialist = await verify.Set<Specialist>()
                .SingleAsync(s => s.Id == id);

            Assert.IsNull(specialist.EncryptedIcalUrl);
            Assert.IsNull(specialist.LastSyncedUtc);
        }

        [TestMethod]
        public async Task DisconnectAsync_LaatKoppelingEnEventsVanAndereSpecialistOngemoeid()
        {
            // Arrange
            var devries = await SpecialistIdAsync(DeVries);
            var bakker = await SpecialistIdAsync(Bakker);
            await SeedAsync(devries, "versleuteld-1", eventCount: 2);
            await SeedAsync(bakker, "versleuteld-2", eventCount: 4);

            await using var context = TestDatabase.CreateDbContext();
            var repository = new CalendarConnectionRepository(context);

            // Act
            await repository.DisconnectAsync(devries);

            // Assert
            Assert.AreEqual((0, 0), await CountsAsync(devries));
            Assert.AreEqual((1, 4), await CountsAsync(bakker));
        }

        [TestMethod]
        public async Task DisconnectAsync_ZonderKoppeling_GooitGeenFoutEnRaaktAndereDataNiet()
        {
            // Arrange
            var devries = await SpecialistIdAsync(DeVries);
            var bakker = await SpecialistIdAsync(Bakker);
            await SeedAsync(bakker, "versleuteld", eventCount: 2);

            await using var context = TestDatabase.CreateDbContext();
            var repository = new CalendarConnectionRepository(context);

            // Act
            await repository.DisconnectAsync(devries);

            // Assert
            Assert.AreEqual((0, 0), await CountsAsync(devries));
            Assert.AreEqual((1, 2), await CountsAsync(bakker));
        }

        [TestMethod]
        public async Task GetConnectionUser_ZonderKoppeling_GeeftSpecialistZonderAgendaTerug()
        {
            // Arrange
            var id = await SpecialistIdAsync(DeVries);
            await using var context = TestDatabase.CreateDbContext();
            var repository = new CalendarConnectionRepository(context);

            // Act
            var result = await repository.GetConnectionUser(id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(id, result.Id);
            Assert.IsNull(result.EncryptedIcalUrl);
        }
    }
}
