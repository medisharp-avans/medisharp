using Server.Models;

namespace Server.Repositories.InterFaces
{
    /// <summary>
    /// Defines data access operations for persisting and synchronizing external calendar events
    /// imported from a specialist's iCal/ICS feed.
    /// </summary>
    public interface ICalendarEventRepository : IRepository<ExternalCalendarEvent>
    {
        /// <summary>
        /// Retrieves all synced external calendar events for a specialist within the specified UTC date range.
        /// </summary>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <param name="fromUtc">The inclusive start of the UTC range.</param>
        /// <param name="toUtc">The inclusive end of the UTC range.</param>
        /// <returns>A list of <see cref="ExternalCalendarEvent"/> entities within the given range.</returns>
        Task<List<ExternalCalendarEvent>> GetBySpecialistAndRangeAsync(int specialistId, DateTime fromUtc, DateTime toUtc);

        /// <summary>
        /// Synchronizes external calendar events within the given UTC window by removing obsolete events,
        /// updating modified events, inserting new events, and updating the connection's last-synced timestamp.
        /// </summary>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <param name="fromUtc">The start of the synchronized UTC window.</param>
        /// <param name="toUtc">The end of the synchronized UTC window.</param>
        /// <param name="fetchedEvents">A dictionary of parsed events keyed by their unique external occurrence identifier.</param>
        /// <param name="connection">The specialist's <see cref="UserCalendarConnection"/> whose <c>LastSyncedUtc</c> timestamp will be updated.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous synchronization operation.</returns>
        Task SyncEventsAsync(
            int specialistId,
            DateTime fromUtc,
            DateTime toUtc,
            Dictionary<string, ExternalCalendarEvent> fetchedEvents,
            UserCalendarConnection connection);
    }
}
