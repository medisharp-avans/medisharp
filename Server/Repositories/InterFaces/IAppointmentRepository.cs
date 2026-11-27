using Server.Models;
using Server.Models.DTO.Requests;

namespace Server.Repositories.InterFaces
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task<List<Appointment>> GetAsync();

        /// <summary>Maakt alleen een afspraak aan, voor een bestaande verwijzing (dto.ReferralId).</summary>
        Task<Appointment> CreateAsync(CreateAppointmentDTO dto);

        /// <summary>
        /// Maakt een verwijzing (status "Ingepland") en de afspraak in één opslagactie aan, dus alles of niets.
        /// dto.ReferralId wordt genegeerd.
        /// </summary>
        /// <exception cref="Server.Exceptions.SlotUnavailableException">Een andere afspraak had het tijdslot net al.</exception>
        Task<Appointment> CreateWithReferralAsync(CreateAppointmentDTO appointment, CreateReferralRequestDTO referral);

        /// <summary>Heeft de specialist een niet-geannuleerde afspraak die overlapt met [start, end)?</summary>
        Task<bool> HasOverlapAsync(int specialistId, DateTime start, DateTime end);

        /// <summary>Valt [start, end] helemaal binnen één beschikbaarheidsblok van de specialist?</summary>
        Task<bool> IsWithinAvailabilityAsync(int specialistId, DateTime start, DateTime end);
    }
}