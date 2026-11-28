using Server.Models;
using Server.Models.DTO.Requests;

namespace Server.Services.InterFaces
{
    /// <summary>
    /// Defines business logic operations related to managing appointments.
    /// </summary>
    public interface IAppointmentService
    {
        /// <summary>
        /// Creates a new appointment manually using the provided request details.
        /// </summary>
        /// <param name="appointment">The data transfer object containing the manual appointment creation details.</param>
        /// <returns>The newly created <see cref="Appointment"/> entity.</returns>
        Task<Appointment> CreateAppointment(AppointmentManualRequestDTO appointment);
    }
}