using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class CalendarConnectionRepository : Repository<UserCalendarConnection>, ICalendarConnectionRepository
    {
        public CalendarConnectionRepository(AppDbContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
        public async Task<UserCalendarConnection?> GetConnectionUser(int specialistId)
        {
            return await _context.UserCalendarConnections.FirstOrDefaultAsync(x => x.SpecialistId == specialistId);
        }

        /// <inheritdoc />
        public async Task<UserCalendarConnection> UpdateUserCalendarConnection(UserCalendarConnection? connection, int specialistId, string encryptedUrl)
        {
            if (connection == null)
            {
                connection = new UserCalendarConnection { SpecialistId = specialistId };
                _context.UserCalendarConnections.Add(connection);
            }
            connection.EncryptedIcalUrl = encryptedUrl;
            await _context.SaveChangesAsync();

            return connection;
        }

        /// <inheritdoc />
        public async Task DisconnectAsync(int specialistId)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                await _context.ExternalCalendarEvents
                    .Where(e => e.SpecialistId == specialistId)
                    .ExecuteDeleteAsync();

                await _context.UserCalendarConnections
                    .Where(c => c.SpecialistId == specialistId)
                    .ExecuteDeleteAsync();

                await tx.CommitAsync();
            });
        }
    }
}
