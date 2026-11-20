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

        /// <summary>Creates a new DbContext on the test database. The caller disposes it.</summary>
        public static AppDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(TestSettings.ConnectionString).Options);

        /// <summary>
        /// Resets the test database to the seed data. The first reset of the test run drops the database
        /// and recreates it with the migrations, so new migrations are always applied.
        /// After that only the tables are emptied, which is much faster.
        /// </summary>
        public static async Task ResetAsync()
        {
            // Safety check: this deletes all data, so never on a database that is not meant for testing
            var databaseName = new SqlConnectionStringBuilder(TestSettings.ConnectionString).InitialCatalog;
            if (!databaseName.EndsWith("Test", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Database '{databaseName}' does not end with 'Test'. The tests delete all data, so they stop.");

            await using var context = CreateDbContext();
            if (_isRecreated)
            {
                await EmptyAllTablesAsync(context);
            }
            else
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.MigrateAsync();
                _isRecreated = true;
            }
            await TestDbInitializer.SeedAsync(context);
            TestLog.Step("Test database reset");
        }

        /// <summary>
        /// Runs a query and returns the rows, for example to check what a test saved.
        /// Pass values as parameters, for example:
        /// <c>QueryAsync("SELECT * FROM Patients WHERE Name = @name", new SqlParameter("@name", "Jan"))</c>.
        /// </summary>
        public static async Task<DataTable> QueryAsync(string sql, params SqlParameter[] parameters)
        {
            TestLog.Step($"[DB] {OneLine(sql)}");
            await using var connection = await OpenConnectionAsync();
            await using var command = CreateCommand(sql, connection, parameters);
            await using var reader = await command.ExecuteReaderAsync();
            var table = new DataTable();
            table.Load(reader);
            return table;
        }

        /// <summary>Runs an INSERT, UPDATE or DELETE and returns the number of rows it changed. Pass values as parameters.</summary>
        public static async Task<int> ExecuteAsync(string sql, params SqlParameter[] parameters)
        {
            TestLog.Step($"[DB] {OneLine(sql)}");
            await using var connection = await OpenConnectionAsync();
            await using var command = CreateCommand(sql, connection, parameters);
            return await command.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Locks a table exclusively until the result is disposed, so the app cannot read or write it.
        /// Use it with 'await using' so the lock is always released. The table must exist, for example "Patients" or "dbo.Patients".
        /// </summary>
        public static async Task<IAsyncDisposable> LockTableAsync(string tableName)
        {
            var connection = await OpenConnectionAsync();
            try
            {
                // A table name cannot be a SQL parameter, so it is looked up in sys.tables
                // and only the quoted name SQL Server returns is put in the query
                await using var lookup = CreateCommand(@"
                    SELECT QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name)
                    FROM sys.tables
                    WHERE object_id = OBJECT_ID(@tableName)", connection, new SqlParameter("@tableName", tableName));
                var quotedName = await lookup.ExecuteScalarAsync() as string
                    ?? throw new ArgumentException($"Table '{tableName}' does not exist in the test database.", nameof(tableName));

                var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
                await using (var command = new SqlCommand($"SELECT TOP 1 1 FROM {quotedName} WITH (TABLOCKX, HOLDLOCK);", connection, transaction))
                    await command.ExecuteNonQueryAsync();

                TestLog.Step($"[DB] Table {quotedName} locked");
                return new TableLock(connection, transaction, quotedName);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        /// <summary>
        /// Deletes all rows (except the migration history) and resets the identity columns,
        /// so the seed data and new rows get the same ids as in a new database.
        /// </summary>
        private static async Task EmptyAllTablesAsync(AppDbContext context)
        {
            var tables = await context.Database.SqlQueryRaw<string>(@"
                SELECT QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) AS Value
                FROM sys.tables
                WHERE name <> '__EFMigrationsHistory'").ToListAsync();
            if (tables.Count == 0) return;

            // Only identity columns that were used before: reseeding a never used one would make the next id 0 instead of 1
            var identityTables = await context.Database.SqlQueryRaw<string>(@"
                SELECT QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name) AS Value
                FROM sys.identity_columns c
                JOIN sys.tables t ON t.object_id = c.object_id
                WHERE c.last_value IS NOT NULL AND t.name <> '__EFMigrationsHistory'").ToListAsync();

            // The names come from sys.tables and are quoted by QUOTENAME, so they are safe to put in the SQL.
            // The foreign keys are switched off while deleting, so the order of the tables does not matter.
            var sql = new StringBuilder();
            foreach (var table in tables)
                sql.AppendLine($"ALTER TABLE {table} NOCHECK CONSTRAINT ALL;");
            foreach (var table in tables)
                sql.AppendLine($"DELETE FROM {table};");
            foreach (var table in tables)
                sql.AppendLine($"ALTER TABLE {table} WITH CHECK CHECK CONSTRAINT ALL;");
            foreach (var table in identityTables)
                sql.AppendLine($"DBCC CHECKIDENT ('{table}', RESEED, 0) WITH NO_INFOMSGS;");

            await context.Database.ExecuteSqlRawAsync(sql.ToString());
            context.ChangeTracker.Clear();
        }

        /// <summary>Opens a new connection to the test database. The caller disposes it.</summary>
        private static async Task<SqlConnection> OpenConnectionAsync()
        {
            var connection = new SqlConnection(TestSettings.ConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        /// <summary>Creates a command with the given parameters on an open connection.</summary>
        private static SqlCommand CreateCommand(string sql, SqlConnection connection, params SqlParameter[] parameters)
        {
            var command = new SqlCommand(sql, connection);
            command.Parameters.AddRange(parameters);
            return command;
        }

        /// <summary>Puts a multi-line SQL statement on one line, for the log.</summary>
        private static string OneLine(string sql) =>
            string.Join(" ", sql.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()));

        /// <summary>Releases the table lock by rolling back its transaction.</summary>
        private sealed class TableLock(SqlConnection connection, SqlTransaction transaction, string tableName) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                await transaction.RollbackAsync();
                await transaction.DisposeAsync();
                await connection.DisposeAsync();
                TestLog.Step($"[DB] Table {tableName} unlocked");
            }
        }
    }
}
