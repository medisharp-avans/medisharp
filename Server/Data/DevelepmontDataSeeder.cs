using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Models.Enums;

namespace Server.Services;

/// <summary>
/// Creates isolated development-only test data on startup and removes it
/// before creating a fresh set. The same data is removed on graceful shutdown.
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
        var specialisms = Enum.GetValues<Specialism>();
        var appointmentStatuses = Enum.GetValues<AppointmentStatus>();

        if (specialisms.Length == 0 || appointmentStatuses.Length == 0)
            throw new InvalidOperationException(
                "Specialism and AppointmentStatus must each contain at least one enum value.");

        var gpUser = new User
        {
            Username = $"{UserPrefix}huisarts",
            Email = "dev-huisarts@example.test",
            PasswordHash = "DEV-ONLY-random-password-hash-01",
            Role = FindEnumValue<UserRole>("GeneralPractitioner", "Huisarts"),
            IsActive = true
        };

        var specialist1User = new User
        {
            Username = $"{UserPrefix}specialist-1",
            Email = "dev-specialist1@example.test",
            PasswordHash = "DEV-ONLY-random-password-hash-02",
            Role = FindEnumValue<UserRole>("Specialist"),
            IsActive = true
        };

        var specialist2User = new User
        {
            Username = $"{UserPrefix}specialist-2",
            Email = "dev-specialist2@example.test",
            PasswordHash = "DEV-ONLY-random-password-hash-03",
            Role = FindEnumValue<UserRole>("Specialist"),
            IsActive = true
        };

        var patient1User = new User
        {
            Username = $"{UserPrefix}patient-1",
            Email = "dev-patient1@example.test",
            PasswordHash = "DEV-ONLY-random-password-hash-04",
            Role = FindEnumValue<UserRole>("Patient"),
            IsActive = true
        };

        var patient2User = new User
        {
            Username = $"{UserPrefix}patient-2",
            Email = "dev-patient2@example.test",
            PasswordHash = "DEV-ONLY-random-password-hash-05",
            Role = FindEnumValue<UserRole>("Patient"),
            IsActive = true
        };

        db.Users.AddRange(gpUser, specialist1User, specialist2User, patient1User, patient2User);
        await db.SaveChangesAsync(cancellationToken);

        var gp = new GeneralPractitioner { UserId = gpUser.Id };
        var specialist1 = new Specialist
        {
            UserId = specialist1User.Id,
            Specialism = specialisms[0]
        };
        var specialist2 = new Specialist
        {
            UserId = specialist2User.Id,
            Specialism = specialisms.Length > 1 ? specialisms[1] : specialisms[0]
        };

        db.GeneralPractitioners.Add(gp);
        db.Specialists.AddRange(specialist1, specialist2);
        await db.SaveChangesAsync(cancellationToken);

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

        var treatmentCodes = new[]
        {
            new TreatmentCode
            {
                Code = $"{TreatmentCodePrefix}CONSULT",
                Description = "Standaard eerste consult (ontwikkeldata)",
                Specialism = specialisms[0].ToString(),
                DurationInQuarters = 4,
                Cost = 75.00m
            },
            new TreatmentCode
            {
                Code = $"{TreatmentCodePrefix}FOLLOWUP",
                Description = "Vervolgconsult (ontwikkeldata)",
                Specialism = specialisms[0].ToString(),
                DurationInQuarters = 2,
                Cost = 45.00m
            },
            new TreatmentCode
            {
                Code = $"{TreatmentCodePrefix}CHECK",
                Description = "Controleafspraak (ontwikkeldata)",
                Specialism = (specialisms.Length > 1 ? specialisms[1] : specialisms[0]).ToString(),
                DurationInQuarters = 2,
                Cost = 35.00m
            }
        };

        db.TreatmentCodes.AddRange(treatmentCodes);
        await db.SaveChangesAsync(cancellationToken);

        var referral1 = new Referral
        {
            PatientId = patient1.Id,
            GeneralPractitionerId = gp.Id,
            Specialism = specialisms[0],
            PreferredSpecialistId = specialist1.Id,
            Reason = "Testverwijzing wegens aanhoudende klachten.",
            Questionnaire = "Ontwikkeldata: klachten bestaan enkele weken.",
            Urgency = "Regulier",
            TreatmentCode = treatmentCodes[0].Code,
            Status = "Open",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        var referral2 = new Referral
        {
            PatientId = patient2.Id,
            GeneralPractitionerId = gp.Id,
            Specialism = specialisms.Length > 1 ? specialisms[1] : specialisms[0],
            PreferredSpecialistId = specialist2.Id,
            Reason = "Testverwijzing voor een controle.",
            Questionnaire = "Ontwikkeldata: periodieke controle.",
            Urgency = "Laag",
            TreatmentCode = treatmentCodes[2].Code,
            Status = "Open",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        db.Referrals.AddRange(referral1, referral2);
        await db.SaveChangesAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var appointments = new[]
        {
            new Appointment
            {
                PatientId = patient1.Id,
                SpecialistId = specialist1.Id,
                ReferralId = referral1.Id,
                StartTime = now.AddDays(1).Date.AddHours(9),
                EndTime = now.AddDays(1).Date.AddHours(10),
                Status = appointmentStatuses[0]
            },
            new Appointment
            {
                PatientId = patient2.Id,
                SpecialistId = specialist2.Id,
                ReferralId = referral2.Id,
                StartTime = now.AddDays(2).Date.AddHours(13),
                EndTime = now.AddDays(2).Date.AddHours(13).AddMinutes(30),
                Status = appointmentStatuses[0]
            }
        };

        db.Appointments.AddRange(appointments);
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
        db.Patients.RemoveRange(patients);
        db.Specialists.RemoveRange(specialists);
        db.GeneralPractitioners.RemoveRange(generalPractitioners);
        db.Users.RemoveRange(users);
        db.TreatmentCodes.RemoveRange(treatmentCodes);

        await db.SaveChangesAsync(cancellationToken);

        // Reset identity counters only when the entire table is empty.
        // This avoids duplicate-key conflicts with non-seed records.
        await ResetIdentityIfEmptyAsync(db, "Users", !await db.Users.AnyAsync(cancellationToken), cancellationToken);
        await ResetIdentityIfEmptyAsync(db, "Patients", !await db.Patients.AnyAsync(cancellationToken), cancellationToken);
        await ResetIdentityIfEmptyAsync(db, "Specialists", !await db.Specialists.AnyAsync(cancellationToken), cancellationToken);
        await ResetIdentityIfEmptyAsync(db, "GeneralPractitioners", !await db.GeneralPractitioners.AnyAsync(cancellationToken), cancellationToken);
        await ResetIdentityIfEmptyAsync(db, "Referrals", !await db.Referrals.AnyAsync(cancellationToken), cancellationToken);
        await ResetIdentityIfEmptyAsync(db, "Appointments", !await db.Appointments.AnyAsync(cancellationToken), cancellationToken);
    }

    private static async Task ResetIdentityIfEmptyAsync(
        AppDbContext db,
        string tableName,
        bool isEmpty,
        CancellationToken cancellationToken)
    {
        if (isEmpty)
        {
            await db.Database.ExecuteSqlRawAsync(
                $"DBCC CHECKIDENT ('[dbo].[{tableName}]', RESEED, 0);",
                cancellationToken);
        }
    }

    private static TEnum FindEnumValue<TEnum>(params string[] preferredNames)
        where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();

        foreach (var preferredName in preferredNames)
        {
            var match = values.FirstOrDefault(value =>
                string.Equals(value.ToString(), preferredName, StringComparison.OrdinalIgnoreCase));

            if (values.Any(value =>
                    string.Equals(value.ToString(), preferredName, StringComparison.OrdinalIgnoreCase)))
                return match;
        }

        // Fallback keeps the seeder compatible with projects that use different enum labels.
        if (values.Length > 0)
            return values[0];

        throw new InvalidOperationException($"Enum {typeof(TEnum).Name} contains no values.");
    }
}
