using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Models.Enums;

namespace Server.Services;

/// <summary>
/// Creates isolated development-only test data on startup and removes it
/// before creating a fresh set. The same data is removed on graceful shutdown.
///
/// Seeded data (all fictitious):
///  - 1 GP (huisarts), 2 specialists (Cardiologie, Dermatologie), 2 patients, 1 hospital employee
///  - 3 treatment codes (DEV-CONSULT, DEV-FOLLOWUP, DEV-CHECK)
///  - 2 referrals: #1 already planned (Ingepland, with an appointment), #2 still Open (to plan)
///  - 1 appointment for referral #1 on the next workday at 09:00
///  - availability 08:00-17:00 for both specialists on the next 10 workdays
/// </summary>
public sealed class DevelopmentDataSeeder(IServiceScopeFactory scopeFactory) : IHostedService
{
    private const string UserPrefix = "dev-seed-";
    private const string TreatmentCodePrefix = "DEV-";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Also removes leftovers from a previous run that did not shut down cleanly.
        await RemoveSeedDataAsync(db, cancellationToken);
        await SeedAsync(db, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await RemoveSeedDataAsync(db, cancellationToken);
    }

    private static async Task SeedAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        // ----- Users -----
        var gpUser = CreateUser("huisarts", UserRole.Huisarts, 1, "Dr.", "Visser");
        var specialist1User = CreateUser("specialist-1", UserRole.Specialist, 2, "Dr.", "Jansen");
        var specialist2User = CreateUser("specialist-2", UserRole.Specialist, 3, "Dr.", "Bakker");
        var patient1User = CreateUser("patient-1", UserRole.Patient, 4, "Emma", "Testpersoon");
        var patient2User = CreateUser("patient-2", UserRole.Patient, 5, "Daan", "Testpersoon");
        var staffUser = CreateUser("medewerker", UserRole.Ziekenhuismedewerker, 6, "Sanne", "Medewerker");

        db.Users.AddRange(gpUser, specialist1User, specialist2User, patient1User, patient2User, staffUser);
        await db.SaveChangesAsync(cancellationToken);

        // ----- General practitioner and specialists -----
        var gp = new GeneralPractitioner { UserId = gpUser.Id };
        var specialist1 = new Specialist { UserId = specialist1User.Id, Specialism = Specialism.Cardiologie };
        var specialist2 = new Specialist { UserId = specialist2User.Id, Specialism = Specialism.Dermatologie };

        db.GeneralPractitioners.Add(gp);
        db.Specialists.AddRange(specialist1, specialist2);
        await db.SaveChangesAsync(cancellationToken);

        // ----- Patients -----
        var patient1 = new Patient
        {
            UserId = patient1User.Id,
            Bsn = "000000001",
            PhoneNumber = "0612345678",
            Address = "Teststraat 1",
            City = "Utrecht",
            GeneralPractitionerId = gp.Id,
            HasInsurance = true
        };

        var patient2 = new Patient
        {
            UserId = patient2User.Id,
            Bsn = "000000002",
            PhoneNumber = "0687654321",
            Address = "Voorbeeldlaan 2",
            City = "Nieuwegein",
            GeneralPractitionerId = gp.Id,
            HasInsurance = false
        };

        db.Patients.AddRange(patient1, patient2);

        // ----- Treatment codes (Specialism is a string here) -----
        var consult = new TreatmentCode
        {
            Code = $"{TreatmentCodePrefix}CONSULT",
            Description = "Standaard eerste consult (ontwikkeldata)",
            Specialism = Specialism.Cardiologie,
            DurationInQuarters = 4,
            Cost = 75.00m
        };

        var followUp = new TreatmentCode
        {
            Code = $"{TreatmentCodePrefix}FOLLOWUP",
            Description = "Vervolgconsult (ontwikkeldata)",
            Specialism = Specialism.Cardiologie,
            DurationInQuarters = 2,
            Cost = 45.00m
        };

        var check = new TreatmentCode
        {
            Code = $"{TreatmentCodePrefix}CHECK",
            Description = "Controleafspraak (ontwikkeldata)",
            Specialism = Specialism.KNO,
            DurationInQuarters = 2,
            Cost = 35.00m
        };

        db.TreatmentCodes.AddRange(consult, followUp, check);
        await db.SaveChangesAsync(cancellationToken);

        // ----- Referrals: #1 is already planned, #2 is still open so it can be planned in the UI -----
        var referral1 = new Referral
        {
            PatientId = patient1.Id,
            GeneralPractitionerId = gp.Id,
            Specialism = Specialism.Cardiologie,
            PreferredSpecialistId = specialist1.Id,
            Reason = "Testverwijzing wegens aanhoudende klachten.",
            Questionnaire = "Ontwikkeldata: klachten bestaan enkele weken.",
            Urgency = "Regulier",
            TreatmentCode = consult.Code,
            Status = "Ingepland",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var referral2 = new Referral
        {
            PatientId = patient2.Id,
            GeneralPractitionerId = gp.Id,
            Specialism = Specialism.Dermatologie,
            PreferredSpecialistId = specialist2.Id,
            Reason = "Testverwijzing voor een controle.",
            Questionnaire = "Ontwikkeldata: periodieke controle.",
            Urgency = "Regulier",
            TreatmentCode = check.Code,
            Status = "Open",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        db.Referrals.AddRange(referral1, referral2);
        await db.SaveChangesAsync(cancellationToken);

        // ----- Availability: 08:00-17:00 on the next 10 workdays for both specialists -----
        var availabilities = new List<Availability>();
        foreach (var specialist in new[] { specialist1, specialist2 })
        {
            for (var workday = 1; workday <= 10; workday++)
            {
                var day = NextWorkday(workday);
                availabilities.Add(new Availability
                {
                    SpecialistId = specialist.Id,
                    StartTime = day.AddHours(8),
                    EndTime = day.AddHours(17)
                });
            }
        }

        db.Availabilities.AddRange(availabilities);

        // ----- Appointment for referral #1 (DEV-CONSULT = 4 quarters = 1 hour) -----
        var appointmentDay = NextWorkday(1);
        db.Appointments.Add(new Appointment
        {
            PatientId = patient1.Id,
            SpecialistId = specialist1.Id,
            ReferralId = referral1.Id,
            StartTime = appointmentDay.AddHours(9),
            EndTime = appointmentDay.AddHours(10),
            Status = AppointmentStatus.Ingepland
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task RemoveSeedDataAsync(
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var seededUserIds = await db.Users
            .Where(u => u.Username.StartsWith(UserPrefix))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var seededPatientIds = await db.Patients
            .Where(p => seededUserIds.Contains(p.UserId))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var seededSpecialistIds = await db.Specialists
            .Where(s => seededUserIds.Contains(s.UserId))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var seededGpIds = await db.GeneralPractitioners
            .Where(g => seededUserIds.Contains(g.UserId))
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);

        var seededTreatmentCodes = await db.TreatmentCodes
            .Where(t => t.Code.StartsWith(TreatmentCodePrefix))
            .Select(t => t.Code)
            .ToListAsync(cancellationToken);

        var seededReferralIds = await db.Referrals
            .Where(r =>
                seededPatientIds.Contains(r.PatientId) ||
                seededGpIds.Contains(r.GeneralPractitionerId) ||
                (r.PreferredSpecialistId.HasValue &&
                 seededSpecialistIds.Contains(r.PreferredSpecialistId.Value)) ||
                seededTreatmentCodes.Contains(r.TreatmentCode))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var appointments = await db.Appointments
            .Where(a =>
                seededPatientIds.Contains(a.PatientId) ||
                seededSpecialistIds.Contains(a.SpecialistId) ||
                (a.ReferralId.HasValue && seededReferralIds.Contains(a.ReferralId.Value)))
            .ToListAsync(cancellationToken);

        var referrals = await db.Referrals
            .Where(r => seededReferralIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var availabilities = await db.Availabilities
            .Where(a => seededSpecialistIds.Contains(a.SpecialistId))
            .ToListAsync(cancellationToken);

        var patients = await db.Patients
            .Where(p => seededPatientIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var specialists = await db.Specialists
            .Where(s => seededSpecialistIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        var generalPractitioners = await db.GeneralPractitioners
            .Where(g => seededGpIds.Contains(g.Id))
            .ToListAsync(cancellationToken);

        var users = await db.Users
            .Where(u => seededUserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var treatmentCodes = await db.TreatmentCodes
            .Where(t => seededTreatmentCodes.Contains(t.Code))
            .ToListAsync(cancellationToken);

        // Delete dependents first because the relationships use NoAction.
        db.Appointments.RemoveRange(appointments);
        db.Referrals.RemoveRange(referrals);
        db.Availabilities.RemoveRange(availabilities);
        db.Patients.RemoveRange(patients);
        db.Specialists.RemoveRange(specialists);
        db.GeneralPractitioners.RemoveRange(generalPractitioners);
        db.Users.RemoveRange(users);
        db.TreatmentCodes.RemoveRange(treatmentCodes);

        await db.SaveChangesAsync(cancellationToken);

        // Reset identity counters only for tables that are now completely empty,
        // so the seed gets the same ids every run and non-seed records are never affected.
        foreach (var table in new[]
                 { "Users", "Patients", "Specialists", "GeneralPractitioners", "Referrals", "Availabilities", "Appointments" })
        {
            await ResetIdentityIfUnusedAsync(db, table, cancellationToken);
        }
    }

    /// <summary>
    /// Reseeds the identity of an empty table to 0, but only if the identity was used before.
    /// Reseeding a never used identity would make the first id 0 instead of 1.
    /// The table names are constants of this class, so they are safe to put in the SQL.
    /// </summary>
    private static Task ResetIdentityIfUnusedAsync(
        AppDbContext db,
        string tableName,
        CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync($@"
            IF NOT EXISTS (SELECT 1 FROM [dbo].[{tableName}])
               AND EXISTS (SELECT 1 FROM sys.identity_columns
                           WHERE object_id = OBJECT_ID('[dbo].[{tableName}]') AND last_value IS NOT NULL)
                DBCC CHECKIDENT ('[dbo].[{tableName}]', RESEED, 0) WITH NO_INFOMSGS;",
            cancellationToken);

    private static User CreateUser(string name, UserRole role, int number, string surName, string lastName) => new()
    {
        Username = $"{UserPrefix}{name}",
        SurName = surName,
        LastName = lastName,
        FullName = $"{surName} {lastName}",
        Email = $"dev-{name}@example.test",
        PasswordHash = $"DEV-ONLY-random-password-hash-{number:00}",
        Role = role,
        IsActive = true
    };

    /// <summary>The date (00:00) of the n-th workday from today, skipping Saturdays and Sundays.</summary>
    private static DateTime NextWorkday(int workdaysAhead)
    {
        var date = DateTime.UtcNow.Date;
        while (workdaysAhead > 0)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                workdaysAhead--;
        }
        return date;
    }
}