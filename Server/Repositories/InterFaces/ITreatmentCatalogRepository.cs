using Server.Models;

namespace Server.Repositories.InterFaces
{
    /// <summary>Provides read-only access to the treatment reference data.</summary>
    public interface ITreatmentCatalogRepository
    {
        /// <summary>Lists the distinct specialties available for referral selection.</summary>
        Task<IReadOnlyList<string>> GetSpecialtiesAsync(CancellationToken cancellationToken);

        /// <summary>Lists treatments belonging to the selected specialty.</summary>
        Task<IReadOnlyList<Treatment>> GetBySpecialtyAsync(string specialty, CancellationToken cancellationToken);

        /// <summary>Finds the authoritative treatment data used to validate a referral.</summary>
        Task<Treatment?> FindByCodeAsync(string code, CancellationToken cancellationToken);
    }
}
