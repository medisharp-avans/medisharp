using Server.Exceptions;
using Server.Models;
using Server.Models.DTO;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Repositories.InterFaces;
using Server.Services.InterFaces;

namespace Server.Services
{
    public class PlanningService : IPlanningService
    {
        private static readonly TimeZoneInfo DutchTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        private readonly IPlanningRepository _repository;
        private readonly ICalculateService _calculateService;

        public PlanningService(IPlanningRepository repository, ICalculateService calculateService)
        {
            _repository = repository;
            _calculateService = calculateService;
        }

        /// <inheritdoc />
        public async Task<List<PlanningResponseDTO>> GetPatientsAsync()
        {
            return await _repository.GetAllPatientsAsync();
        }

        /// <inheritdoc />
        public async Task<List<TreatmentCode>> GetCodesBySpecialist(string spec)
        {
            return await _repository.GetCodesBySpecialism(spec);
        }

        /// <inheritdoc />
        public async Task<List<PlanningResponseDTO>> GetSpecialistsBySpecialism(string spec)
        {
            return await _repository.GetSpecialistsBySpecialism(spec);
        }

        /// <inheritdoc />
        /// <exception cref="SlotUnavailableException">Thrown when no suitable specialist is found within the 3-week window.</exception>
        public async Task<SpecialistAppointmentDTO> SetSpecialistForAppointment(AppointmentManualRequestDTO dto)
        {
            // Step 1: Determine the exact appointment duration based on the treatment code's quarter-hour units.
            // Throws KeyNotFoundException if the treatment code does not exist.
            var baseReference = DateTime.Today;
            var calculatedEnd = await _calculateService.CalculateAppointmentEndTime(baseReference, dto.TreatmentCode);
            var duration = calculatedEnd - baseReference;

            // Step 2: Build the ordered list of candidate specialists.
            // Priority rule: If a preferred specialist is specified, evaluate their entire 3-week window first.
            // If none is provided or no slot is available, fall back to all other specialists in the requested specialism.
            var candidateSpecialistIds = new List<int>();

            if (dto.PreferredSpecialistId.HasValue)
            {
                candidateSpecialistIds.Add(dto.PreferredSpecialistId.Value);
            }

            var specialistsInSpecialism = await _repository.GetSpecialistsBySpecialism(dto.Specialism.ToString());
            foreach (var specialist in specialistsInSpecialism)
            {
                if (specialist.SpecialistId.HasValue &&
                    !candidateSpecialistIds.Contains(specialist.SpecialistId.Value))
                {
                    candidateSpecialistIds.Add(specialist.SpecialistId.Value);
                }
            }

            // Step 3: Define the 3-week (21-day) search window starting tomorrow at 00:00 Dutch local time.
            var dutchNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, DutchTimeZone);
            var windowStart = dutchNow.Date.AddDays(1);
            var windowEnd = windowStart.AddDays(21);

            // Step 4: Iterate through candidate specialists in priority order and return the earliest valid slot found.
            foreach (var specialistId in candidateSpecialistIds)
            {
                var slot = await FindFirstAvailableSlotAsync(specialistId, windowStart, windowEnd, duration);
                if (slot != null)
                {
                    return slot;
                }
            }

            // Step 5: No specialist had an open slot within the 3-week window.
            throw new SlotUnavailableException(
                "Geen beschikbare specialist gevonden binnen de termijn van 3 weken.");
        }

        /// <summary>
        /// Scans a single specialist's schedule in 15-minute increments across workdays (Mon-Fri, 09:00-17:00)
        /// within the 21-day window and returns the first conflict-free time slot.
        /// </summary>
        private async Task<SpecialistAppointmentDTO?> FindFirstAvailableSlotAsync(
            int specialistId,
            DateTime windowStart,
            DateTime windowEnd,
            TimeSpan duration)
        {
            // Fetch all combined busy intervals (internal appointments + external iCal events) in one query
            // to avoid N+1 database calls inside the time-slot loop.
            var busyIntervals = await _repository.GetBusyIntervalsAsync(specialistId, windowStart, windowEnd);

            // Iterate day by day through the 3-week (21-day) window.
            for (int dayOffset = 0; dayOffset < 21; dayOffset++)
            {
                var currentDay = windowStart.AddDays(dayOffset);

                // Skip weekends: appointments can only be scheduled Monday through Friday.
                if (currentDay.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    continue;

                // Define working hours for the current day (09:00 to 17:00).
                var workStart = currentDay.AddHours(9);
                var workEnd = currentDay.AddHours(17);

                var candidateStart = workStart;

                // Slide through the workday in 15-minute steps.
                // Ensure the full appointment (including its end time) finishes at or before 17:00.
                while (candidateStart + duration <= workEnd)
                {
                    var candidateEnd = candidateStart + duration;

                    // Standard interval-overlap check: [A_start, A_end) overlaps [B_start, B_end) iff A_start < B_end && A_end > B_start.
                    // This catches all conflict cases:
                    // 1. Candidate starts inside an existing event.
                    // 2. Candidate starts while free, but its end time spills over into the start of a later event.
                    // 3. Candidate completely envelops a shorter existing event.
                    // Back-to-back events (e.g., busy ends at 10:30 and candidate starts at 10:30) do NOT conflict.
                    bool hasConflict = busyIntervals.Any(busy =>
                        busy.Start < candidateEnd && busy.End > candidateStart);

                    if (!hasConflict)
                    {
                        return new SpecialistAppointmentDTO
                        {
                            SpecialistId = specialistId,
                            StartTime = candidateStart,
                            EndTime = candidateEnd
                        };
                    }

                    // Advance to the next 15-minute slot.
                    candidateStart = candidateStart.AddMinutes(15);
                }
            }

            // No free slot found for this specialist within the 3-week window.
            return null;
        }
    }
}
