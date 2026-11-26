using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class PlanningRepository : Repository<Patient>, IPlanningRepository
    {
        public PlanningRepository(AppDbContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
        public async Task<List<PlanningResponseDTO>> GetAllPatientsAsync()
        {
            return await _context.Patients
                .Select(p => new PlanningResponseDTO
                {
                    patientId = p.Id,
                    patientFullName = p.User.FullName,
                })
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<TreatmentCode>> GetCodesBySpecialism(string spec)
        {
            if (!Enum.TryParse<Specialism>(spec, true, out var parsed))
                return new List<TreatmentCode>();

            return await _context.TreatmentCodes
                .Where(t => t.Specialism == parsed)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<List<PlanningResponseDTO>> GetSpecialistsBySpecialism(string spec)
        {
            if (!Enum.TryParse<Specialism>(spec, true, out var parsed))
                return new List<PlanningResponseDTO>();

            return await _context.Specialists
                .Where(s => s.Specialism == parsed)
                .Select(s => new PlanningResponseDTO
                {
                    SpecialistId = s.Id,
                    SpecialistFullName = s.User.FullName,
                    Specialism = s.Specialism.ToString()
                })
                .ToListAsync();
        }

        /// <inheritdoc />
        public Task<bool> HasOverlapAsync(int specialistId, DateTime start, DateTime end) =>
            _context.Appointments.AnyAsync(a =>
                a.SpecialistId == specialistId
                && a.Status != AppointmentStatus.Geannuleerd
                && a.StartTime < end
                && a.EndTime > start);

        /// <inheritdoc />
        public Task<bool> IsWithinAvailabilityAsync(int specialistId, DateTime start, DateTime end) =>
            _context.Availabilities.AnyAsync(a =>
                a.SpecialistId == specialistId
                && a.StartTime <= start
                && a.EndTime >= end);
    }
}
