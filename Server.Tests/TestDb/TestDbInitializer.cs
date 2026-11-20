using Microsoft.EntityFrameworkCore;

namespace Server.Tests.TestDb
{
    /// <summary>
    /// Fills the empty test database with the fixed test data of the test cases.
    /// Runs before every database and end-to-end test, right after the tables were emptied.
    /// </summary>
    public static class TestDbInitializer
    {
        public static void Seed(DbContext context)
        {
            // Add the test data here once AppDbContext has entities, for example:
            // context.Database.ExecuteSqlRaw(@"
            //     SET IDENTITY_INSERT Patients ON;
            //     INSERT INTO Patients (Id, Name) VALUES (1, 'Jan de Vries');
            //     SET IDENTITY_INSERT Patients OFF;");
            // Fixed ids keep the test cases readable ("patient 1"). Use ExecuteSql (not Raw) for values
            // such as password hashes, so they are passed as SQL parameters.

            context.ChangeTracker.Clear();
        }
    }
}
