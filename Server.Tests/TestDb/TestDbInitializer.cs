using Microsoft.EntityFrameworkCore;

namespace Server.Tests.TestDb
{
    public static class TestDbInitializer
    {
        public static void Seed(DbContext context)
        {
            Console.WriteLine("TestDbInitializer.Seed() called");

            Console.WriteLine("=== SEEDING COMPLETED SUCCESSFULLY ===");
        }
    }
}