using Microsoft.AspNetCore.DataProtection;
using Server.Models;

namespace Server.Repositories.InterFaces
{
    public interface ICalendarConnectionRepository : IRepository<UserCalendarConnection>
    {
        Task<UserCalendarConnection?> GetConnectionUser(int userId);
        Task<UserCalendarConnection> UpdateUserCalendarConnection(UserCalendarConnection connection, int userId, string url);
        Task DisconnectAsync(int specialistId);
    }
}
