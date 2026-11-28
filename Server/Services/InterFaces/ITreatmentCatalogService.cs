using Server.Models.DTO.Responses;

namespace Server.Services.InterFaces
{
    /// <summary>Provides the choices needed by the referral form.</summary>
    public interface ITreatmentCatalogService
    {
        /// <summary>Returns all available specialty names.</summary>
        Task<IReadOnlyList<string>> GetSpecialtiesAsync(CancellationToken cancellationToken);

        /// <summary>Returns treatment choices for one specialty, without exposing database entities.</summary>
        Task<IReadOnlyList<TreatmentResponse>> GetBySpecialtyAsync(
            string specialty, CancellationToken cancellationToken);
    }
}
