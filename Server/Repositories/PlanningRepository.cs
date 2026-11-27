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
        private static readonly TimeZoneInfo DutchTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

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
                .Where(t => t.Specialism == parsed && t.IsActive)
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
        public async Task<List<(DateTime Start, DateTime End)>> GetBusyIntervalsAsync(
            int specialistId,
            DateTime windowStart,
            DateTime windowEnd)
        {
            // Step 1: Fetch all active (non-cancelled) internal appointments overlapping the search window.
            // Internal appointments are already stored in Dutch local time (Europe/Amsterdam).
            var appointments = await _context.Appointments
                .Where(a => a.SpecialistId == specialistId
                    && a.Status != AppointmentStatus.Geannuleerd
                    && a.StartTime < windowEnd
                    && a.EndTime > windowStart)
                .Select(a => new { a.StartTime, a.EndTime })
                .ToListAsync();

            // Step 2: Convert the local search window to UTC so we can accurately query external calendar events,
            // which are persisted in UTC.
            var windowStartUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(windowStart, DateTimeKind.Unspecified), DutchTimeZone);
            var windowEndUtc = TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(windowEnd, DateTimeKind.Unspecified), DutchTimeZone);

            // Step 3: Fetch all synced external calendar events (e.g., Google/Outlook iCal) overlapping the UTC window.
            var externalEvents = await _context.ExternalCalendarEvents
                .Where(e => e.SpecialistId == specialistId
                    && e.StartUtc < windowEndUtc
                    && e.EndUtc > windowStartUtc)
                .Select(e => new { e.StartUtc, e.EndUtc })
                .ToListAsync();

            // Step 4: Normalize all busy blocks into a single unified list in Dutch local time.
            var busyIntervals = new List<(DateTime Start, DateTime End)>(
                appointments.Count + externalEvents.Count);

            foreach (var app in appointments)
            {
                busyIntervals.Add((app.StartTime, app.EndTime));
            }

            foreach (var ext in externalEvents)
            {
                // Convert UTC timestamps from the external calendar back to Europe/Amsterdam local time
                // (automatically accounting for Daylight Saving Time / CET vs. CEST).
                var startLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(ext.StartUtc, DateTimeKind.Utc), DutchTimeZone);
                var endLocal = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(ext.EndUtc, DateTimeKind.Utc), DutchTimeZone);

                busyIntervals.Add((startLocal, endLocal));
            }

            return busyIntervals;
        }
    }
}
