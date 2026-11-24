using Server.Models;

namespace Server.Repositories.InterFaces
{
    /// <summary>
    /// Provides data access operations specific to <see cref="TreatmentCode"/> entities.
    /// </summary>
    public interface ITreatmentCodeRepository : IRepository<TreatmentCode>
    {
        /// <summary>
        /// Retrieves a treatment code entity by its unique string code asynchronously.
        /// </summary>
        /// <param name="code">The treatment code string to search for.</param>
        /// <returns>The matching <see cref="TreatmentCode"/> if found; otherwise, <c>null</c>.</returns>
        Task<TreatmentCode?> GetByCodeAsync(string code);
    }
}