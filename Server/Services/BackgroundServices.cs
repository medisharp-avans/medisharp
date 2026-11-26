using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services.InterFaces;

namespace Server.Services
{
    public class CalendarBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<CalendarBackgroundWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
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
                            logger.LogWarning(ex, "Agenda-sync mislukt voor specialist {SpecialistId}", conn.SpecialistId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Onverwachte fout in CalendarBackgroundWorker.");
                }
            }
        }
    }
}
