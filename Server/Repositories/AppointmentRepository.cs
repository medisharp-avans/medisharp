using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Exceptions;
using Server.Models;
using Server.Models.DTO.Requests;
using Server.Models.Enums;
using Server.Repositories.InterFaces;

namespace Server.Repositories
{
    public class AppointmentRepository : Repository<Appointment>, IAppointmentRepository
    {
        // SQL Server: 2601 = unique index geschonden, 2627 = unique constraint geschonden
        private const int UniqueIndexViolation = 2601;
        private const int UniqueConstraintViolation = 2627;

        public AppointmentRepository(AppDbContext context)
            : base(context)
        {
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public async Task<Appointment> CreateWithReferralAsync(
            CreateAppointmentDTO appointmentDto,
            CreateReferralRequestDTO referralDto)
        {
            var referral = new Referral
            {
                PatientId = referralDto.PatientId,
                GeneralPractitionerId = referralDto.GeneralPractitionerId,
                Specialism = referralDto.Specialism,
                PreferredSpecialistId = referralDto.PreferredSpecialistId,
                Reason = referralDto.Reason,
                Questionnaire = referralDto.Questionnaire ?? string.Empty,
                Urgency = referralDto.Urgency,
                TreatmentCode = referralDto.TreatmentCode,
                Status = "Ingepland",
                CreatedAt = DateTime.UtcNow
            };

            var appointment = new Appointment
            {
                PatientId = appointmentDto.PatientId,
                SpecialistId = appointmentDto.SpecialistId,
                StartTime = appointmentDto.StartTime,
                EndTime = appointmentDto.EndTime,
                Status = AppointmentStatus.Ingepland,
                Referral = referral
            };

            _context.Appointments.Add(appointment);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                throw new SlotUnavailableException("Dit tijdslot is niet meer beschikbaar.", ex);
            }

            return appointment;
        }

        private static bool IsUniqueViolation(DbUpdateException ex) =>
            ex.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation };
    }
}