using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Server.Tests.Utils;
using System.Data;

namespace Server.Tests.TestDb
{
    public class TestMsSqlDatabaseHelper
    {
        private readonly string _connectionString;

        public TestMsSqlDatabaseHelper(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Executes a SQL query (SELECT, INSERT, UPDATE, DELETE).
        /// </summary>
        /// <param name="query">The SQL command to execute.</param>
        /// <returns>A DataTable containing results for SELECT queries, otherwise empty.</returns>
        public DataTable ExecuteQuery(string query)
        {
            DataTable dataTable = new DataTable();
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    using (var command = new SqlCommand(query, connection))
                    {
                        // For SELECT queries, use a DataAdapter to fetch results
                        if (query.Trim().ToUpper().StartsWith("SELECT"))
                        {
                            using (var adapter = new SqlDataAdapter(command))
                            {
                                adapter.Fill(dataTable);
                            }
                        }
                        // For modification queries (INSERT, UPDATE, DELETE)
                        else
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Implement proper error logging here
                    Console.WriteLine($"Database Error: {ex.Message}");
                    throw; // Re-throw the exception to fail the test
                }
            }
            return dataTable;
        }

        /// <summary>
        /// Dropt de database als die bestaat, en voert daarna migrate + seed uit.
        /// Bedoeld voor test-omgevingen: ALLE DATA GAAT VERLOREN.
        /// </summary>
        public void ResetDatabase(DbContext context)
        {
            context.Database.CloseConnection();
            context.ChangeTracker.Clear();
            SqlConnection.ClearAllPools();

            // FIX: Gebruik de ingebouwde EF Core functies. 
            // Dit verwijdert gegarandeerd de JUISTE database die in de DbContext zit,
            // zonder ooit de 'master' database aan te raken of SINGLE_USER triggers te forceren.
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // Opnieuw aanmaken via migrations en seeden
            //TestDbInitializer.Seed(context);
        }

        /// <summary>
        /// Leegt alle tabellen (behalve __EFMigrationsHistory) en reset identity-tellers.
        /// Het schema blijft bestaan; sneller dan de database droppen.
        /// </summary>
        public void ClearData()
        {
            const string sql = @"
                DECLARE @sql nvarchar(max) = N'';

                SELECT @sql += 'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ' NOCHECK CONSTRAINT ALL;'
                FROM sys.tables WHERE name <> '__EFMigrationsHistory';

                SELECT @sql += 'DELETE FROM ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ';'
                FROM sys.tables WHERE name <> '__EFMigrationsHistory';

                SELECT @sql += 'DBCC CHECKIDENT (''' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ''', RESEED, 0);'
                FROM sys.tables
                WHERE name <> '__EFMigrationsHistory' AND OBJECTPROPERTY(object_id, 'TableHasIdentity') = 1;

                SELECT @sql += 'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(schema_id)) + '.' + QUOTENAME(name) + ' WITH CHECK CHECK CONSTRAINT ALL;'
                FROM sys.tables WHERE name <> '__EFMigrationsHistory';

                EXEC sp_executesql @sql;";

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
            TestLog.Step($"Database succesvol geleegd voor volgende test");
        }
    }
}

