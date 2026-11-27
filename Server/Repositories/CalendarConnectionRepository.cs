
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class CalendarConnectionRepository : Repository<Specialist>, ICalendarConnectionRepository
    {
        public CalendarConnectionRepository(AppDbContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
        public async Task<Specialist?> GetConnectionUser(int specialistId)
        {
            return await _context.Specialists.FirstOrDefaultAsync(x => x.Id == specialistId);
        }

        /// <inheritdoc />
        public async Task<Specialist> UpdateUserCalendarConnection(Specialist specialist, string encryptedUrl)
        {
            specialist.EncryptedIcalUrl = encryptedUrl;
            await _context.SaveChangesAsync();

            return specialist;
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

                var specialist = await _context.Specialists
                    .FirstOrDefaultAsync(s => s.Id == specialistId);

                if (specialist != null)
                {
                    specialist.EncryptedIcalUrl = null;
                    specialist.LastSyncedUtc = null;

                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
            });
        }
    }
}
