using Server.Repositories;
using Server.Repositories.InterFaces;
using Server.Models;

namespace Server.Tests.Fixtures
{
    /// <summary>Small per-test catalogue for referral business rules without a database connection.</summary>
    public class StubTreatmentCatalogRepository : ITreatmentCatalogRepository
    {
        public List<Treatment> Treatments { get; } =
        [
            new() { Code = "Z0005", Description = "Consult oren", Specialty = "KNO", DurationInQuarters = 3, CostEuro = 65m },
            new() { Code = "Z0001", Description = "Consult hart", Specialty = "Cardiologie", DurationInQuarters = 3, CostEuro = 144m }
        ];

        public Task<IReadOnlyList<string>> GetSpecialtiesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<string>>(Treatments.Select(treatment => treatment.Specialty).Distinct().ToArray());

        public Task<IReadOnlyList<Treatment>> GetBySpecialtyAsync(string specialty, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Treatment>>(Treatments.Where(treatment => treatment.Specialty == specialty).ToArray());

        public Task<Treatment?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(Treatments.SingleOrDefault(treatment => treatment.Code == code));
    }
}
