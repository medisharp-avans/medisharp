using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Models;
using Server.Models.Enums;

namespace Server.Data;

/// <summary>Adds the fictitious accounts used to test sign-in in Development.</summary>
internal static class DevelopmentAccountSeeder
{
    private const string DevPassword = "DevWachtwoord123!";

    /// <summary>Creates missing demo accounts and preserves existing account data.</summary>
    public static async Task SeedAsync(
        AppDbContext db, IPasswordHasher<User> passwordHasher, CancellationToken cancellationToken)
    {
        User[] accounts =
        [
            CreateAccount("huisarts", UserRole.Huisarts, "Dr.", "Visser"),
            CreateAccount("specialist-1", UserRole.Specialist, "Dr.", "Jansen"),
            CreateAccount("specialist-2", UserRole.Specialist, "Dr.", "Bakker"),
            CreateAccount("patient-1", UserRole.Patient, "Emma", "Testpersoon"),
            CreateAccount("patient-2", UserRole.Patient, "Daan", "Testpersoon"),
            CreateAccount("medewerker", UserRole.Ziekenhuismedewerker, "Sanne", "Medewerker")
        ];
        var demoEmails = accounts.Select(account => account.Email).ToArray();
        var existingEmails = await db.Users.Where(user => demoEmails.Contains(user.Email))
            .Select(user => user.Email)
            .ToListAsync(cancellationToken);

        foreach (var account in accounts)
        {
            if (existingEmails.Contains(account.Email, StringComparer.OrdinalIgnoreCase)) continue;

            account.PasswordHash = passwordHasher.HashPassword(account, DevPassword);
            db.Users.Add(account);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static User CreateAccount(string username, UserRole role, string firstName, string lastName) =>
        new()
        {
            Email = $"dev-{username}@example.test",
            Role = role,
            SurName = firstName,
            LastName = lastName,
            FullName = $"{firstName} {lastName}"
        };
}
