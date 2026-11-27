using Microsoft.AspNetCore.Identity;
using Server.Data;
using Server.Models;
using Server.Models.Enums;

namespace Server.Tests.TestDb
{
    /// <summary>Seeds the two fictitious accounts used in the referral workflow test.</summary>
    public static class TestDbInitializer
    {
        public const string Password = "Test123!";
        public const string GpEmail = "visser@example.test";
        public const string StaffEmail = "medewerker.a@example.test";

        public static async Task SeedAsync(AppDbContext context)
        {
            var gp = new User
            {
                SurName = "Dr.",
                LastName = "Visser",
                FullName = "Dr. Visser",
                Email = GpEmail,
                Role = UserRole.Huisarts
            };
            var staff = new User
            {
                SurName = "Medewerker",
                LastName = "A",
                FullName = "Medewerker A",
                Email = StaffEmail,
                Role = UserRole.Ziekenhuismedewerker
            };

            var passwordHasher = new PasswordHasher<User>();
            gp.PasswordHash = passwordHasher.HashPassword(gp, Password);
            staff.PasswordHash = passwordHasher.HashPassword(staff, Password);
            context.Users.AddRange(gp, staff);
            await context.SaveChangesAsync();
        }
    }
}
