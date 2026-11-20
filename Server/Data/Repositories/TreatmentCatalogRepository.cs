using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data.Repositories
{
    /// <summary>Queries the catalogue with typed, asynchronous EF Core queries.</summary>
    public class TreatmentCatalogRepository(AppDbContext context) : ITreatmentCatalogRepository
    {
        /// <inheritdoc />
        public async Task<IReadOnlyList<string>> GetSpecialtiesAsync(CancellationToken cancellationToken) =>
            await context.Treatments.AsNoTracking()
                .Select(treatment => treatment.Specialty)
                .Distinct()
                .OrderBy(specialty => specialty)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<Treatment>> GetBySpecialtyAsync(
            string specialty, CancellationToken cancellationToken) =>
            await context.Treatments.AsNoTracking()
                .Where(treatment => treatment.Specialty == specialty)
                .OrderBy(treatment => treatment.Description)
                .ThenBy(treatment => treatment.Code)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public Task<Treatment?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
            context.Treatments.AsNoTracking()
                .SingleOrDefaultAsync(treatment => treatment.Code == code, cancellationToken);
    }
}
