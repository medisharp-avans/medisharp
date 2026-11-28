using Server.Models;
using Server.Models.DTO.Requests;

namespace Server.Repositories.InterFaces
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        /// <summary>
        /// Creates a new appointment for an existing referral specified by <paramref name="dto"/>.ReferralId.
        /// </summary>
        /// <param name="dto">The data transfer object containing appointment creation details.</param>
        /// <returns>The newly created <see cref="Appointment"/> entity.</returns>
        Task<Appointment> CreateAsync(CreateAppointmentDTO dto);

        /// <summary>
        /// Creates both a referral (with the status "Ingepland") and the appointment in a single atomic database transaction, ensuring all-or-nothing execution.
        /// Note that any provided ReferralId within the appointment DTO will be ignored.
        /// </summary>
        /// <param name="appointment">The appointment details to create.</param>
        /// <param name="referral">The referral details to create alongside the appointment.</param>
        /// <returns>The newly created <see cref="Appointment"/> entity.</returns>
        /// <exception cref="Server.Exceptions.SlotUnavailableException">Thrown when the requested time slot was just taken by another appointment.</exception>
        Task<Appointment> CreateWithReferralAsync(CreateAppointmentDTO appointment, CreateReferralRequestDTO referral);
    }
}
