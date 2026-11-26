using Microsoft.EntityFrameworkCore;
using Server.Models;
using Server.Models.Enums;

namespace Server.Tests.TestDb
{
    /// <summary>
    /// Fills the empty test database with the fixed claims of the DEC test cases:
    /// four claims (POLIS-AAA to POLIS-DDD, letters only so a search on a claim number never matches them), two of them reviewed by Sanne Medewerker.
    /// </summary>
    public static class TestDECDataInitializer
    {
        public static readonly DateTime Now = new(2026, 11, 26, 12, 0, 0, DateTimeKind.Utc);

        public static async Task SeedAsync(DbContext context)
        {
            // Users
            var janUser = CreateUser("jan.jansen", "Jan", "Jansen", UserRole.Patient);
            var emmaUser = CreateUser("emma.devries", "Emma", "de Vries", UserRole.Patient);
            var pietUser = CreateUser("piet.bakker", "Piet", "Bakker", UserRole.Patient);
            var gpUser = CreateUser("huisarts.visser", "Dr.", "Visser", UserRole.Huisarts);
            var specialistUser = CreateUser("dr.devries", "Dr.", "De Vries", UserRole.Specialist);
            var staffUser = CreateUser("sanne.medewerker", "Sanne", "Medewerker", UserRole.Ziekenhuismedewerker);

            context.Set<User>().AddRange(janUser, emmaUser, pietUser, gpUser, specialistUser, staffUser);
            await context.SaveChangesAsync();

            // General practitioner, specialist and hospital employee
            var gp = new GeneralPractitioner { UserId = gpUser.Id };
            var specialist = new Specialist { UserId = specialistUser.Id, Specialism = Specialism.Cardiologie };
            var staff = new HospitalEmployee { UserId = staffUser.Id, Department = "Administratie" };

            context.Set<GeneralPractitioner>().Add(gp);
            context.Set<Specialist>().Add(specialist);
            context.Set<HospitalEmployee>().Add(staff);
            await context.SaveChangesAsync();

            // Patients
            var jan = CreatePatient(janUser, gp, "100000001");
            var emma = CreatePatient(emmaUser, gp, "100000002");
            var piet = CreatePatient(pietUser, gp, "100000003");

            context.Set<Patient>().AddRange(jan, emma, piet);
            await context.SaveChangesAsync();

            // Completed appointments: one per claim line, because an appointment is claimed at most once
            var appointments = Enumerable.Range(1, 5)
                .Select(day => new Appointment
                {
                    PatientId = jan.Id,
                    SpecialistId = specialist.Id,
                    StartTime = Now.Date.AddDays(-day).AddHours(9),
                    EndTime = Now.Date.AddDays(-day).AddHours(10),
                    Status = AppointmentStatus.Voltooid
                })
                .ToList();

            context.Set<Appointment>().AddRange(appointments);
            await context.SaveChangesAsync();

            // Claims
            context.Set<Claim>().AddRange(
                CreateClaim("POLIS-AAA", jan, ClaimStatus.Processed, lastModifiedDaysAgo: 0, staff, (appointments[0], 75m)),
                CreateClaim("POLIS-BBB", emma, ClaimStatus.ToReview, lastModifiedDaysAgo: 5, null, (appointments[1], 300m), (appointments[2], 120m)),
                CreateClaim("POLIS-CCC", piet, ClaimStatus.ReadyToSend, lastModifiedDaysAgo: 1, staff, (appointments[3], 45m)),
                CreateClaim("POLIS-DDD", jan, ClaimStatus.ToReview, lastModifiedDaysAgo: 1, null, (appointments[4], 35m)));
            await context.SaveChangesAsync();
        }

        private static User CreateUser(string username, string surName, string lastName, UserRole role) => new()
        {
            Username = username,
            SurName = surName,
            LastName = lastName,
            FullName = $"{surName} {lastName}",
            PasswordHash = "fake-hash-niet-echt",
            Email = $"{username}@test.nl",
            Role = role
        };

        private static Patient CreatePatient(User user, GeneralPractitioner gp, string bsn) => new()
        {
            UserId = user.Id,
            Bsn = bsn,
            PhoneNumber = "0612345678",
            Address = "Teststraat 1",
            City = "Breda",
            GeneralPractitionerId = gp.Id,
            HasInsurance = true
        };

        private static Claim CreateClaim(
            string policyNumber,
            Patient patient,
            ClaimStatus status,
            int lastModifiedDaysAgo,
            HospitalEmployee? reviewedBy,
            params (Appointment Appointment, decimal Amount)[] lines) => new()
        {
            PatientId = patient.Id,
            PolicyNumber = policyNumber,
            Status = status,
            ReviewedById = reviewedBy?.Id,
            CreatedAt = Now.AddDays(-10),
            LastModifiedAt = Now.AddDays(-lastModifiedDaysAgo),
            ClaimLines = [.. lines.Select(l => new ClaimLine { AppointmentId = l.Appointment.Id, SubmittedAmount = l.Amount })]
        };
    }
}
