using Server.Models;
using Server.Models.DTO.Requests;
using Server.Repositories.InterFaces;
using Server.Services.InterFaces;

namespace Server.Services
{
    public class ReferralService : IReferralService
    {
        private readonly IReferralRepository _repository;

        public ReferralService(IReferralRepository repository)
        {
            _repository = repository;
        }

        /// <inheritdoc />
        public async Task<Referral> CreateAsync(CreateReferralRequestDTO dto)
        {
            return await _repository.CreateAsync(dto);
        }
    }
}