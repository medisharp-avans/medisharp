using Server.Models;

namespace Server.Services.InterFaces
{
    /// <summary>
    /// Defines operations for connecting, synchronizing, checking, and disconnecting
    /// a specialist's external iCal/ICS calendar feed.
    /// </summary>
    public interface IAgendaService
    {
        /// <summary>
        /// Normalizes, validates, encrypts, and stores a specialist's external iCal/ICS URL,
        /// and performs an immediate initial synchronization of calendar events.
        /// </summary>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <param name="rawUrl">The raw iCal/ICS feed URL provided by the specialist (supports <c>https://</c> and <c>webcal://</c>).</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous save and synchronization operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the URL is unreachable or does not return valid iCalendar content.</exception>
        Task SaveAndSyncUrlAsync(int specialistId, string rawUrl);

        /// <summary>
        /// Fetches, parses, and synchronizes external calendar occurrences (including recurring <c>RRULE</c> events)
        /// for an existing calendar connection into the database.
        /// </summary>
        /// <param name="connection">The specialist's stored calendar connection containing the encrypted iCal URL.</param>
        /// <param name="preloadedIcs">Optional pre-fetched iCalendar content to avoid a duplicate HTTP request during initial connection.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous synchronization operation.</returns>
        Task SyncUserEventsAsync(UserCalendarConnection connection, string? preloadedIcs = null);

        /// <summary>
        /// Checks whether the specified specialist currently has an active external calendar connection.
        /// </summary>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <returns><c>true</c> if an external calendar is linked; otherwise, <c>false</c>.</returns>
        Task<bool> IsConnectedAsync(int specialistId);

        /// <summary>
        /// Removes the specialist's external calendar connection and deletes all associated synced calendar events.
        /// </summary>
        /// <param name="specialistId">The unique identifier of the specialist.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous disconnect and cleanup operation.</returns>
        Task DisconnectAsync(int specialistId);
    }
}
