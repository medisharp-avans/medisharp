using Server.Models;

namespace Server.Services.InterFaces
{
    public interface IAgendaService
    {
        Task SaveAndSyncUrlAsync(int userId, string rawUrl);
        Task SyncUserEventsAsync(UserCalendarConnection connection, string? preloadedIcs = null);
        Task<bool> IsConnectedAsync(int specialistId);
        Task DisconnectAsync(int specialistId);
    }
}
