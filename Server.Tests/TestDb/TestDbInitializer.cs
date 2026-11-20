using Microsoft.EntityFrameworkCore;

namespace Server.Tests.TestDb
{
    /// <summary>
    /// Fills the empty test database with the fixed test data of the test cases.
    /// Runs before every database and end-to-end test, right after the tables were emptied.
    /// </summary>
    public static class TestDbInitializer
    {
        /// <summary>Inserts the seed data. Only use fictitious data here (DoD 19).</summary>
        public static Task SeedAsync(DbContext context)
        {
            // Add the test data here once AppDbContext has entities, make this method 'async' and await the inserts, for example:
            // await context.Database.ExecuteSqlRawAsync(@"
            //     SET IDENTITY_INSERT Patients ON;
            //     INSERT INTO Patients (Id, Name) VALUES (1, 'Jan de Vries');
            //     SET IDENTITY_INSERT Patients OFF;");
            // Fixed ids keep the test cases readable ("patient 1"). Use ExecuteSqlAsync (not Raw) for values
            // such as password hashes, so they are passed as SQL parameters.

            context.ChangeTracker.Clear();
            return Task.CompletedTask;
        }
    }
}
