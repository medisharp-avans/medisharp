
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class CalendarEventRepository : Repository<ExternalCalendarEvent>, ICalendarEventRepository
    {
        public CalendarEventRepository(AppDbContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
        public async Task<List<ExternalCalendarEvent>> GetBySpecialistAndRangeAsync(int specialistId, DateTime fromUtc, DateTime toUtc)
        {
            return await _context.ExternalCalendarEvents
                .Where(e => e.SpecialistId == specialistId && e.StartUtc >= fromUtc && e.StartUtc <= toUtc)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task SyncEventsAsync(
            int specialistId,
            DateTime fromUtc,
            DateTime toUtc,
            Dictionary<string, ExternalCalendarEvent> fetchedEvents,
            Specialist connection)
        {
            var existingEvents = await GetBySpecialistAndRangeAsync(specialistId, fromUtc, toUtc);

            // 1. Delete appointments that are not within the Ical feed annymore
            var toDelete = existingEvents
                .Where(e => !fetchedEvents.ContainsKey(e.ExternalUid))
                .ToList();

            _context.ExternalCalendarEvents.RemoveRange(toDelete);

            // 2. Update existing or add new one
            var existingMap = existingEvents.ToDictionary(e => e.ExternalUid);
            foreach (var (uid, incoming) in fetchedEvents)
            {
                if (existingMap.TryGetValue(uid, out var current))
                {
                    current.Title = incoming.Title;
                    current.StartUtc = incoming.StartUtc;
                    current.EndUtc = incoming.EndUtc;
                    current.IsAllDay = incoming.IsAllDay;
                }
                else
                {
                    _context.ExternalCalendarEvents.Add(incoming);
                }
            }

            // 3. Resync and save within one transaction
            if (_context.Entry(connection).State == EntityState.Detached)
                _context.Specialists.Update(connection);

            connection.LastSyncedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}
