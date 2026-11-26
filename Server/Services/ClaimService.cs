using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Repositories.InterFaces;
using Server.Services.InterFaces;

namespace Server.Services
{
    public class ClaimService : IClaimService
    {
        private readonly IClaimRepository _repository;

        public ClaimService(IClaimRepository repository)
        {
            _repository = repository;
        }

        /// <inheritdoc />
        public async Task<List<ClaimOverviewDTO>> GetOverviewAsync()
        {
            var claims = await _repository.GetAllWithDetailsAsync();

            return claims
                .OrderBy(c => c.Status == ClaimStatus.ToReview ? 0 : 1)
                .ThenByDescending(c => c.LastModifiedAt)
                .Select(c => new ClaimOverviewDTO(
                    c.Id,
                    c.PatientId,
                    c.Patient?.User?.FullName ?? string.Empty,
                    c.PolicyNumber,
                    c.Status,
                    c.ClaimLines.Count,
                    c.ClaimLines.Sum(l => l.SubmittedAmount),
                    c.CreatedAt,
                    c.SubmittedAt,
                    c.LastModifiedAt))
                .ToList();
        }
    }
}
