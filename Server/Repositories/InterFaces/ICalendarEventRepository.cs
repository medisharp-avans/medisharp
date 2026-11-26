using Server.Models;

namespace Server.Repositories.InterFaces
{
    public interface ICalendarEventRepository : IRepository<ExternalCalendarEvent>
    {
        Task<List<ExternalCalendarEvent>> GetBySpecialistAndRangeAsync(int specialistId, DateTime fromUtc, DateTime toUtc);
        Task SyncEventsAsync(
            int specialistId,
            DateTime fromUtc,
            DateTime toUtc,
            Dictionary<string, ExternalCalendarEvent> fetchedEvents,
            UserCalendarConnection connection);
    }
}
