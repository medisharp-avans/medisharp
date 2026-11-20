using Server.Contracts.Treatments;
using Server.Data.Repositories;

namespace Server.Services.Treatments
{
    /// <summary>Maps catalogue records to the public reference data contract.</summary>
    public class TreatmentCatalogService(ITreatmentCatalogRepository repository) : ITreatmentCatalogService
    {
        /// <inheritdoc />
        public Task<IReadOnlyList<string>> GetSpecialtiesAsync(CancellationToken cancellationToken) =>
            repository.GetSpecialtiesAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<IReadOnlyList<TreatmentResponse>> GetBySpecialtyAsync(
            string specialty, CancellationToken cancellationToken)
        {
            var treatments = await repository.GetBySpecialtyAsync(specialty.Trim(), cancellationToken);
            return treatments.Select(treatment => new TreatmentResponse
            {
                Code = treatment.Code,
                Description = treatment.Description,
                Specialty = treatment.Specialty,
                DurationInQuarters = treatment.DurationInQuarters,
                CostEuro = treatment.CostEuro
            }).ToArray();
        }
    }
}
