using Microsoft.EntityFrameworkCore;
using Server.Models;
using Server.Models.Enums;

namespace Server.Data;

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
///  - 5 completed appointments in the past for patient #1, claimed in 4 claims
///    (2x ToReview, 1x ReadyToSend, 1x Processed) for the claims overview (DEC-01)
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
        var gp = CreateGeneralPractitioner(gpUser);
        var specialist1 = CreateSpecialist(specialist1User, Specialism.Cardiologie);
        var specialist2 = CreateSpecialist(specialist2User, Specialism.Dermatologie);

        db.GeneralPractitioners.Add(gp);
        db.Specialists.AddRange(specialist1, specialist2);

        // ----- Hospital employee who reviews claims -----
        var staff = new HospitalEmployee { UserId = staffUser.Id, Department = "Administratie" };
        db.HospitalEmployees.Add(staff);
        await db.SaveChangesAsync(cancellationToken);

        // ----- Patients (only patient #1 is insured) -----
        var patient1 = CreatePatient(patient1User, gp, "000000001", "0612345678", "Teststraat 1", "Utrecht", hasInsurance: true);
        var patient2 = CreatePatient(patient2User, gp, "000000002", "0687654321", "Voorbeeldlaan 2", "Nieuwegein", hasInsurance: false);

        db.Patients.AddRange(patient1, patient2);

        // ----- Treatment codes -----
        var consult = CreateTreatmentCode("CONSULT", "Standaard eerste consult (ontwikkeldata)", Specialism.Cardiologie, durationInQuarters: 4, cost: 75.00m);
        var followUp = CreateTreatmentCode("FOLLOWUP", "Vervolgconsult (ontwikkeldata)", Specialism.Cardiologie, durationInQuarters: 2, cost: 45.00m);
        var check = CreateTreatmentCode("CHECK", "Controleafspraak (ontwikkeldata)", Specialism.KNO, durationInQuarters: 2, cost: 35.00m);

        db.TreatmentCodes.AddRange(consult, followUp, check);
        await db.SaveChangesAsync(cancellationToken);

        // ----- Referrals: #1 is already planned, #2 is still open so it can be planned in the UI -----
        var referral1 = CreateReferral(patient1, gp, specialist1, consult, "Ingepland", daysAgo: 2,
            "Testverwijzing wegens aanhoudende klachten.", "Ontwikkeldata: klachten bestaan enkele weken.");
        var referral2 = CreateReferral(patient2, gp, specialist2, check, "Open", daysAgo: 1,
            "Testverwijzing voor een controle.", "Ontwikkeldata: periodieke controle.");

        db.Referrals.AddRange(referral1, referral2);
        await db.SaveChangesAsync(cancellationToken);

        // ----- Availability: 08:00-17:00 on the next 10 workdays for both specialists -----
        foreach (var specialist in new[] { specialist1, specialist2 })
        {
            for (var workday = 1; workday <= 10; workday++)
            {
                db.Availabilities.Add(CreateAvailability(specialist, NextWorkday(workday)));
            }
        }

        // ----- Planned appointment for referral #1 on the next workday at 09:00 -----
        db.Appointments.Add(CreateAppointment(patient1, specialist1, consult, NextWorkday(1).AddHours(9),
            AppointmentStatus.Ingepland, referral1));

        await db.SaveChangesAsync(cancellationToken);

        // ----- Completed appointments in the past for patient #1 -----
        // Each claim line needs its own appointment, because an appointment is claimed at most once.
        var today = DateTime.UtcNow.Date;
        var pastConsult1 = CreateAppointment(patient1, specialist1, consult, today.AddDays(-10).AddHours(9), AppointmentStatus.Voltooid);
        var pastFollowUp1 = CreateAppointment(patient1, specialist1, followUp, today.AddDays(-10).AddHours(11), AppointmentStatus.Voltooid);
        var pastCheck = CreateAppointment(patient1, specialist1, check, today.AddDays(-7).AddHours(9), AppointmentStatus.Voltooid);
        var pastFollowUp2 = CreateAppointment(patient1, specialist1, followUp, today.AddDays(-4).AddHours(9), AppointmentStatus.Voltooid);
        var pastConsult2 = CreateAppointment(patient1, specialist1, consult, today.AddDays(-2).AddHours(9), AppointmentStatus.Voltooid);

        db.Appointments.AddRange(pastConsult1, pastFollowUp1, pastCheck, pastFollowUp2, pastConsult2);
        await db.SaveChangesAsync(cancellationToken);

        // ----- Claims for the claims overview (DEC-01): 1x Processed, 1x ReadyToSend, 2x ToReview -----
        var processedClaim = CreateClaim(patient1, ClaimStatus.Processed, createdDaysAgo: 9, lastModifiedDaysAgo: 8,
            [CreateReimbursedClaimLine(pastConsult1, consult, reimbursedAmount: 50.00m, submittedDaysAgo: 8)],
            submittedDaysAgo: 8, reviewedBy: staff);

        var readyToSendClaim = CreateClaim(patient1, ClaimStatus.ReadyToSend, createdDaysAgo: 6, lastModifiedDaysAgo: 5,
            [CreateClaimLine(pastCheck, check)], reviewedBy: staff);

        // Two treatment codes on one claim (AC-DEC-01_1): 45 + 75 = 120.
        var toReviewClaimTwoLines = CreateClaim(patient1, ClaimStatus.ToReview, createdDaysAgo: 2, lastModifiedDaysAgo: 2,
            [CreateClaimLine(pastFollowUp2, followUp), CreateClaimLine(pastConsult2, consult)]);

        var toReviewClaimOneLine = CreateClaim(patient1, ClaimStatus.ToReview, createdDaysAgo: 9, lastModifiedDaysAgo: 9,
            [CreateClaimLine(pastFollowUp1, followUp)]);

        db.Claims.AddRange(processedClaim, readyToSendClaim, toReviewClaimTwoLines, toReviewClaimOneLine);
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

        var claims = await db.Claims
            .Include(c => c.ClaimLines)
            .Where(c => seededPatientIds.Contains(c.PatientId))
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

        var hospitalEmployees = await db.HospitalEmployees
            .Where(h => seededUserIds.Contains(h.UserId))
            .ToListAsync(cancellationToken);

        var users = await db.Users
            .Where(u => seededUserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var treatmentCodes = await db.TreatmentCodes
            .Where(t => seededTreatmentCodes.Contains(t.Code))
            .ToListAsync(cancellationToken);

        // Delete dependents first because the relationships use NoAction.
        db.ClaimLines.RemoveRange(claims.SelectMany(c => c.ClaimLines));
        db.Claims.RemoveRange(claims);
        db.Appointments.RemoveRange(appointments);
        db.Referrals.RemoveRange(referrals);
        db.Availabilities.RemoveRange(availabilities);
        db.Patients.RemoveRange(patients);
        db.Specialists.RemoveRange(specialists);
        db.GeneralPractitioners.RemoveRange(generalPractitioners);
        db.HospitalEmployees.RemoveRange(hospitalEmployees);
        db.Users.RemoveRange(users);
        db.TreatmentCodes.RemoveRange(treatmentCodes);

        await db.SaveChangesAsync(cancellationToken);

        // Reset identity counters only for tables that are now completely empty,
        // so the seed gets the same ids every run and non-seed records are never affected.
        foreach (var table in new[]
                 { "claim_lines", "claims", "hospital_employees", "Users", "Patients", "Specialists", "GeneralPractitioners", "Referrals", "Availabilities", "Appointments" })
        {
            await ResetIdentityIfUnusedAsync(db, table, cancellationToken);
        }
    }

    // EF1002: the interpolated value is always one of the constant table names in this class, never user input,
    // and a table name cannot be passed as a SQL parameter, so ExecuteSqlAsync is not an option here.
#pragma warning disable EF1002
    /// <summary>
    /// Reseeds the identity of an empty table to 0, but only if the identity was used before.
    /// Reseeding a never used identity would make the first id 0 instead of 1.
    /// The table names are constants of this class, so they are safe to put in the SQL.
    /// </summary>
    private static Task<int> ResetIdentityIfUnusedAsync(
        AppDbContext db,
        string tableName,
        CancellationToken cancellationToken) =>
        db.Database.ExecuteSqlRawAsync($@"
            IF NOT EXISTS (SELECT 1 FROM [dbo].[{tableName}])
               AND EXISTS (SELECT 1 FROM sys.identity_columns
                           WHERE object_id = OBJECT_ID('[dbo].[{tableName}]') AND last_value IS NOT NULL)
                DBCC CHECKIDENT ('[dbo].[{tableName}]', RESEED, 0) WITH NO_INFOMSGS;",
            cancellationToken);
#pragma warning restore EF1002

    // ===== Helpers to create the seed entities =====

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

    private static GeneralPractitioner CreateGeneralPractitioner(User user) => new()
    {
        UserId = user.Id
    };

    private static Specialist CreateSpecialist(User user, Specialism specialism) => new()
    {
        UserId = user.Id,
        Specialism = specialism
    };

    private static Patient CreatePatient(
        User user,
        GeneralPractitioner gp,
        string bsn,
        string phoneNumber,
        string address,
        string city,
        bool hasInsurance) => new()
    {
        UserId = user.Id,
        Bsn = bsn,
        PhoneNumber = phoneNumber,
        Address = address,
        City = city,
        GeneralPractitionerId = gp.Id,
        HasInsurance = hasInsurance
    };

    /// <summary>The code gets the DEV- prefix, so it is removed again with the rest of the seed data.</summary>
    private static TreatmentCode CreateTreatmentCode(
        string name,
        string description,
        Specialism specialism,
        int durationInQuarters,
        decimal cost) => new()
    {
        Code = $"{TreatmentCodePrefix}{name}",
        Description = description,
        Specialism = specialism,
        DurationInQuarters = durationInQuarters,
        Cost = cost
    };

    /// <summary>A regular referral to the preferred specialist, for that specialist's specialism.</summary>
    private static Referral CreateReferral(
        Patient patient,
        GeneralPractitioner gp,
        Specialist preferredSpecialist,
        TreatmentCode treatmentCode,
        string status,
        int daysAgo,
        string reason,
        string questionnaire) => new()
    {
        PatientId = patient.Id,
        GeneralPractitionerId = gp.Id,
        Specialism = preferredSpecialist.Specialism,
        PreferredSpecialistId = preferredSpecialist.Id,
        Reason = reason,
        Questionnaire = questionnaire,
        Urgency = "Regulier",
        TreatmentCode = treatmentCode.Code,
        Status = status,
        CreatedAt = DateTime.UtcNow.AddDays(-daysAgo)
    };

    /// <summary>Availability from 08:00 to 17:00 on the given day.</summary>
    private static Availability CreateAvailability(Specialist specialist, DateTime day) => new()
    {
        SpecialistId = specialist.Id,
        StartTime = day.AddHours(8),
        EndTime = day.AddHours(17)
    };

    /// <summary>The end time follows from the duration of the treatment code (in quarters of an hour).</summary>
    private static Appointment CreateAppointment(
        Patient patient,
        Specialist specialist,
        TreatmentCode treatmentCode,
        DateTime startTime,
        AppointmentStatus status,
        Referral? referral = null) => new()
    {
        PatientId = patient.Id,
        SpecialistId = specialist.Id,
        ReferralId = referral?.Id,
        StartTime = startTime,
        EndTime = startTime.AddMinutes(treatmentCode.DurationInQuarters * 15),
        Status = status
    };

    private static Claim CreateClaim(
        Patient patient,
        ClaimStatus status,
        int createdDaysAgo,
        int lastModifiedDaysAgo,
        List<ClaimLine> claimLines,
        int? submittedDaysAgo = null,
        HospitalEmployee? reviewedBy = null) => new()
    {
        PatientId = patient.Id,
        ReviewedById = reviewedBy?.Id,
        PolicyNumber = "DEV-POLIS-0001",
        Status = status,
        CreatedAt = DateTime.UtcNow.AddDays(-createdDaysAgo),
        LastModifiedAt = DateTime.UtcNow.AddDays(-lastModifiedDaysAgo),
        SubmittedAt = submittedDaysAgo.HasValue ? DateTime.UtcNow.AddDays(-submittedDaysAgo.Value) : null,
        ClaimLines = claimLines
    };

    /// <summary>A claim line that has not been submitted yet, for the full cost of the treatment code.</summary>
    private static ClaimLine CreateClaimLine(Appointment appointment, TreatmentCode treatmentCode) => new()
    {
        AppointmentId = appointment.Id,
        SubmittedAmount = treatmentCode.Cost
    };

    /// <summary>A claim line that was submitted once and partially reimbursed by the insurer.</summary>
    private static ClaimLine CreateReimbursedClaimLine(
        Appointment appointment,
        TreatmentCode treatmentCode,
        decimal reimbursedAmount,
        int submittedDaysAgo) => new()
    {
        AppointmentId = appointment.Id,
        SubmittedAmount = treatmentCode.Cost,
        ReimbursedAmount = reimbursedAmount,
        Status = ClaimLineStatus.PartiallyReimbursed,
        SubmittedAt = DateTime.UtcNow.AddDays(-submittedDaysAgo),
        SubmissionCount = 1
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
