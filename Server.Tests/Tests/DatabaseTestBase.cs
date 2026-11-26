using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Tests.TestDb;

namespace Server.Tests.Tests
{
    /// <summary>
    /// Base of integration tests: every test starts with a test database that only has the seed data.
    /// </summary>
    public abstract class DatabaseTestBase : TestBase
    {
        /// <summary>A DbContext on the test database, new for every test.</summary>
        protected AppDbContext DbContext { get; private set; } = null!;

        protected virtual Func<DbContext, Task>? GetTestDataInitializer()
        {
            return null;
        }

        /// <summary>Resets the test database to the seed data and creates the DbContext for this test.</summary>
        [TestInitialize]
        public async Task ResetDatabaseAsync()
        {
            await TestDatabase.ResetAsync(GetTestDataInitializer());
            DbContext = TestDatabase.CreateDbContext();
        }

        /// <summary>Disposes the DbContext of this test.</summary>
        [TestCleanup]
        public async Task DisposeDbContextAsync()
        {
            if (DbContext != null)
                await DbContext.DisposeAsync();
        }
    }
}
