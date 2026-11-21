using Server.Models;
using Server.Models.DTO.Requests;

namespace Server.Repositories.InterFaces
{
    public interface IAppointmentRepository : IRepository<Appointment>
    {
        Task<List<Appointment>> GetAsync();
        Task<Appointment> CreateAsync(CreateAppointmentDTO dto);
    }
}
