using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Server.Data;
using Server.Models;
using Server.Models.DTO.Requests;
using Server.Models.Enums;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
    {
        public AppointmentRepository(AppDbContext context)
            : base(context)
        {
        }

        public async Task<List<Appointment>> GetAsync()
        {
            return await _context.Appointments.ToListAsync();
        }

        public async Task<Appointment> CreateAsync(CreateAppointmentDTO dto)
        {
            var appointment = new Appointment
            {
                PatientId = dto.PatientId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                SpecialistId = dto.SpecialistId,
                Status = AppointmentStatus.Ingepland,
                ReferralId = dto.ReferralId
            };

            _context.Add(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }
    }
}
