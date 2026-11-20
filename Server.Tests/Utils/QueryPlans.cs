using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using System.Data;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Server.Tests.Utils
{
    public class QueryPlans
    {
        private readonly AppDbContext _context;

        public QueryPlans(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        private static void Info(string template, params object?[] args) =>
            TestLog.Logger.Information("    [DB] " + template, args);

        private static void Debug(string template, params object?[] args) =>
            TestLog.Logger.Debug("    [DB] " + template, args);

        private static void Warn(string template, params object?[] args) =>
            TestLog.Logger.Warning("    [DB] " + template, args);

        public void Execute(string sql)
        {
            var oneLine = string.Join(" ", sql.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                                              .Select(l => l.Trim()));
            Info("SQL uitvoeren: {Sql}", oneLine);

            var connection = _context.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
                connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = sql;
            var rows = command.ExecuteNonQuery();
            Debug("SQL klaar, {Rows} rij(en) geraakt", rows);
        }

        public IDisposable LockTable(string tableName)
        {
            var connectionString = _context.Database.GetConnectionString();
            var connection = new SqlConnection(connectionString);
            connection.Open();

            var transaction = connection.BeginTransaction();
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = $"SELECT TOP 1 1 FROM {tableName} WITH (TABLOCKX, HOLDLOCK);";
                command.ExecuteNonQuery();
            }

            Info("Tabel '{Table}' exclusief vergrendeld (TABLOCKX), wordt vrijgegeven bij Dispose", tableName);
            return new TableLockResource(connection, transaction, tableName);
        }

        private class TableLockResource : IDisposable
        {
            private readonly SqlConnection _connection;
            private readonly SqlTransaction _transaction;
            private readonly string _tableName;

            public TableLockResource(SqlConnection connection, SqlTransaction transaction, string tableName)
            {
                _connection = connection;
                _transaction = transaction;
                _tableName = tableName;
            }

            public void Dispose()
            {
                _transaction?.Rollback();
                _transaction?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
                Info("Vergrendeling van tabel '{Table}' vrijgegeven (rollback)", _tableName);
            }
        }
    }
}