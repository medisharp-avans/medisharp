using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services.InterFaces;

namespace Server.Services
{
    /// <summary>
    /// Background worker service that periodically synchronizes external iCal/ICS calendars
    /// for all connected specialists into the local database.
    /// </summary>
    /// <param name="scopeFactory">Factory used to create scoped service providers for resolving EF Core and scoped services.</param>
    /// <param name="logger">Logger used to record synchronization warnings and unexpected background errors.</param>
    /// <param name="syncInterval">Interval used to determine the consistent times the database has to be resynced. On default its 15 minutes</param>
    public class CalendarBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<CalendarBackgroundWorker> logger,
        TimeSpan? syncInterval = null) : BackgroundService
    {
        private readonly TimeSpan _interval = syncInterval ?? TimeSpan.FromMinutes(15);

        /// <summary>
        /// Executes the periodic synchronization loop every 15 minutes until the application shuts down.
        /// </summary>
        /// <param name="stoppingToken">Triggered when the application host is performing a graceful shutdown.</param>
        /// <returns>A <see cref="Task"/> representing the lifetime of the background operation.</returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Run the synchronization cycle at a fixed 15-minute interval.
            using var timer = new PeriodicTimer(_interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    // Create a new DI scope per cycle because AppDbContext and IAgendaService are registered as scoped services.
                    using var scope = scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var syncService = scope.ServiceProvider.GetRequiredService<IAgendaService>();

                    var connections = await db.UserCalendarConnections.ToListAsync(stoppingToken);

                    foreach (var conn in connections)
                    {
                        try
                        {
                            await syncService.SyncUserEventsAsync(conn);
                        }
                        catch (Exception ex)
                        {
                            // Log per-specialist failures (e.g., revoked or unreachable iCal URL) without interrupting other specialists.
                            logger.LogWarning(ex, "Agenda-sync mislukt voor specialist {SpecialistId}", conn.SpecialistId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Catch unexpected cycle-level errors (e.g., transient database outages) so the host does not terminate.
                    logger.LogError(ex, "Onverwachte fout in CalendarBackgroundWorker.");
                }
            }
        }
    }
}
