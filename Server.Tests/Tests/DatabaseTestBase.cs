using Server.Data;
using Server.Tests.TestDb;

namespace Server.Tests
{
    /// <summary>
    /// Base of integration tests: every test starts with a test database that only has the seed data.
    /// </summary>
    public abstract class DatabaseTestBase : TestBase
    {
        protected AppDbContext DbContext { get; private set; } = null!;

        [TestInitialize]
        public void ResetDatabase()
        {
            TestDatabase.Reset();
            DbContext = TestDatabase.CreateDbContext();
        }

        [TestCleanup]
        public void DisposeDbContext() => DbContext?.Dispose();
    }
}
