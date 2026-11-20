using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Tests.Utils;
using System.Data;
using System.Text;

namespace Server.Tests.TestDb
{
    /// <summary>
    /// Access to the test database: reset it to the seed data, run SQL to arrange or check data,
    /// and lock a table to test how the app handles a database that does not respond.
    /// </summary>
    public static class TestDatabase
    {
        // True once the database was recreated in this test run
        private static bool _isRecreated;

        public static AppDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(TestSettings.ConnectionString).Options);

        /// <summary>
        /// Resets the test database to the seed data. The first reset of the test run drops the database
        /// and recreates it with the migrations, so new migrations are always applied.
        /// After that only the tables are emptied, which is much faster.
        /// </summary>
        public static void Reset()
        {
            // Safety check: this deletes all data, so never on a database that is not meant for testing
            var databaseName = new SqlConnectionStringBuilder(TestSettings.ConnectionString).InitialCatalog;
            if (!databaseName.EndsWith("Test", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Database '{databaseName}' does not end with 'Test'. The tests delete all data, so they stop.");

            using var context = CreateDbContext();
            if (_isRecreated)
            {
                EmptyAllTables(context);
            }
            else
            {
                context.Database.EnsureDeleted();
                context.Database.Migrate();
                _isRecreated = true;
            }
            TestDbInitializer.Seed(context);
            TestLog.Step("Test database reset");
        }

        /// <summary>Runs a query and returns the rows, for example to check what a test saved.</summary>
        public static DataTable Query(string sql)
        {
            TestLog.Step($"[DB] {OneLine(sql)}");
            using var connection = new SqlConnection(TestSettings.ConnectionString);
            using var adapter = new SqlDataAdapter(sql, connection);
            var table = new DataTable();
            adapter.Fill(table);
            return table;
        }

        /// <summary>Runs an INSERT, UPDATE or DELETE and returns the number of rows it changed.</summary>
        public static int Execute(string sql)
        {
            TestLog.Step($"[DB] {OneLine(sql)}");
            using var connection = new SqlConnection(TestSettings.ConnectionString);
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            return command.ExecuteNonQuery();
        }

        /// <summary>
        /// Locks a table exclusively until the result is disposed, so the app cannot read or write it.
        /// Use it with 'using' so the lock is always released.
        /// </summary>
        public static IDisposable LockTable(string tableName)
        {
            var connection = new SqlConnection(TestSettings.ConnectionString);
            connection.Open();
            var transaction = connection.BeginTransaction();
            using (var command = new SqlCommand($"SELECT TOP 1 1 FROM {tableName} WITH (TABLOCKX, HOLDLOCK);", connection, transaction))
                command.ExecuteNonQuery();

            TestLog.Step($"[DB] Table '{tableName}' locked");
            return new TableLock(connection, transaction, tableName);
        }

        // Deletes all rows (except the migration history) and resets the identity columns,
        // so the seed data and new rows get the same ids as in a new database
        private static void EmptyAllTables(AppDbContext context)
        {
            var tables = context.Database.SqlQueryRaw<string>(@"
                SELECT QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) AS Value
                FROM sys.tables
                WHERE name <> '__EFMigrationsHistory'").ToList();
            if (tables.Count == 0) return;

            // Only identity columns that were used before: reseeding a never used one would make the next id 0 instead of 1
            var identityTables = context.Database.SqlQueryRaw<string>(@"
                SELECT QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name) AS Value
                FROM sys.identity_columns c
                JOIN sys.tables t ON t.object_id = c.object_id
                WHERE c.last_value IS NOT NULL AND t.name <> '__EFMigrationsHistory'").ToList();

            // The foreign keys are switched off while deleting, so the order of the tables does not matter
            var sql = new StringBuilder();
            foreach (var table in tables)
                sql.AppendLine($"ALTER TABLE {table} NOCHECK CONSTRAINT ALL;");
            foreach (var table in tables)
                sql.AppendLine($"DELETE FROM {table};");
            foreach (var table in tables)
                sql.AppendLine($"ALTER TABLE {table} WITH CHECK CHECK CONSTRAINT ALL;");
            foreach (var table in identityTables)
                sql.AppendLine($"DBCC CHECKIDENT ('{table}', RESEED, 0) WITH NO_INFOMSGS;");

            context.Database.ExecuteSqlRaw(sql.ToString());
            context.ChangeTracker.Clear();
        }

        private static string OneLine(string sql) =>
            string.Join(" ", sql.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()));

        private sealed class TableLock(SqlConnection connection, SqlTransaction transaction, string tableName) : IDisposable
        {
            public void Dispose()
            {
                transaction.Rollback();
                transaction.Dispose();
                connection.Dispose();
                TestLog.Step($"[DB] Table '{tableName}' unlocked");
            }
        }
    }
}
