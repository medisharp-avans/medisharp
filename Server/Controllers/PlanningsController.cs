using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models.Enums;

namespace Server.Controllers
{
    /// <summary>
    /// Leesendpoints voor de planningspagina (patiënt, behandelcodes, tijdsloten).
    /// Bewust zonder service of repository: de queries staan direct in de controller.
    /// De routes zijn absoluut, zodat ze naast de bestaande controllers werken.
    /// </summary>
    [ApiController]
    public class PlanningController(AppDbContext db) : ControllerBase
    {
        // Geneste records, zodat er geen losse DTO-bestanden nodig zijn
        public record PatientDto(int Id, string Name, int? GeneralPractitionerId);
        public record TreatmentCodeDto(string Code, string Description, int DurationInQuarters);
        public record TimeSlotDto(int SpecialistId, string SpecialistName, DateTime StartTime);

        // GET /api/patients?search=jan
        [HttpGet("api/patients")]
        public async Task<ActionResult<List<PatientDto>>> SearchPatients([FromQuery] string? search)
        {
            var query = db.Patients.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.User.FullName.Contains(search));

            var patients = await query
                .OrderBy(p => p.User.FullName)
                .Select(p => new PatientDto(p.Id, p.User.FullName, p.GeneralPractitionerId))
                .ToListAsync();

            return Ok(patients);
        }

        // GET /api/patients/1
        [HttpGet("api/patients/{id:int}")]
        public async Task<ActionResult<PatientDto>> GetPatient(int id)
        {
            var patient = await db.Patients.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new PatientDto(p.Id, p.User.FullName, p.GeneralPractitionerId))
                .FirstOrDefaultAsync();

            return patient is null ? NotFound() : Ok(patient);
        }

        // GET /api/treatmentcodes?specialism=KNO
        [HttpGet("api/treatmentcodes")]
        public async Task<ActionResult<List<TreatmentCodeDto>>> GetTreatmentCodes([FromQuery] Specialism specialism)
        {
            // TreatmentCode.Specialism is een string met de naam van de enum
            var name = specialism.ToString();

            var codes = await db.TreatmentCodes.AsNoTracking()
                .Where(t => t.Specialism == name)
                .OrderBy(t => t.Code)
                .Select(t => new TreatmentCodeDto(t.Code, t.Description, t.DurationInQuarters))
                .ToListAsync();

            return Ok(codes);
        }

        // GET /api/timeslots?specialism=KNO&treatmentCode=KNO002&from=2027-01-11&days=14
        [HttpGet("api/timeslots")]
        public async Task<ActionResult<List<TimeSlotDto>>> GetTimeSlots(
            [FromQuery] Specialism specialism,
            [FromQuery] string treatmentCode,
            [FromQuery(Name = "from")] DateOnly fromDate,
            [FromQuery] int days = 14)
        {
            if (days is < 1 or > 60) return BadRequest("days moet tussen 1 en 60 liggen.");

            var code = await db.TreatmentCodes.AsNoTracking().FirstOrDefaultAsync(t => t.Code == treatmentCode);
            if (code is null) return NotFound($"Zorgcode {treatmentCode} bestaat niet.");

            // De stap tussen tijdsloten is de duur van de behandeling (4 kwartier = 60 minuten)
            var duration = TimeSpan.FromMinutes(code.DurationInQuarters * 15);
            var rangeStart = fromDate.ToDateTime(TimeOnly.MinValue);
            var rangeEnd = rangeStart.AddDays(days);

            var specialists = await db.Specialists.AsNoTracking()
                .Where(s => s.Specialism == specialism)
                .Select(s => new { s.Id, s.User.FullName })
                .ToListAsync();
            var specialistIds = specialists.Select(s => s.Id).ToList();

            var availabilities = await db.Availabilities.AsNoTracking()
                .Where(a => specialistIds.Contains(a.SpecialistId) && a.StartTime < rangeEnd && a.EndTime > rangeStart)
                .ToListAsync();

            // Geannuleerde afspraken houden een tijdslot niet bezet
            var busy = await db.Appointments.AsNoTracking()
                .Where(a => specialistIds.Contains(a.SpecialistId)
                            && a.Status != AppointmentStatus.Geannuleerd
                            && a.StartTime < rangeEnd && a.EndTime > rangeStart)
                .Select(a => new { a.SpecialistId, a.StartTime, a.EndTime })
                .ToListAsync();

            var now = DateTime.Now;
            var slots = new List<TimeSlotDto>();

            foreach (var availability in availabilities)
            {
                var name = specialists.First(s => s.Id == availability.SpecialistId).FullName;

                for (var start = availability.StartTime; start + duration <= availability.EndTime; start += duration)
                {
                    // Buiten de gevraagde periode of in het verleden
                    if (start < rangeStart || start >= rangeEnd || start <= now) continue;

                    var end = start + duration;
                    var overlaps = busy.Any(b => b.SpecialistId == availability.SpecialistId
                                                 && b.StartTime < end && b.EndTime > start);
                    if (!overlaps)
                        slots.Add(new TimeSlotDto(availability.SpecialistId, name, start));
                }
            }

            return Ok(slots.OrderBy(s => s.SpecialistName).ThenBy(s => s.StartTime).ToList());
        }
    }
}