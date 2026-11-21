using Server.Models;
using Server.Models.DTO.Requests;

namespace Server.Services.InterFaces
{
    public interface IAppointmentService
    {
        Task<List<Appointment>> GetAppointmentAsync();
        Task<Appointment> CreateAppointment(AppointmentManualRequestDTO appointment);
    }
}
