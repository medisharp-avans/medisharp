
using Microsoft.Data.SqlClient;

namespace Server.Tests.TestDb.QueryHelpers
{
    /// <summary>
    /// Assert via de database voor AFS-07.
    /// Tabellen volgen de EF-conventie voor de modellen Specialist en ExternalCalendarEvent.
    /// Heten de DbSets of tabellen anders, pas dan de namen hieronder aan.
    /// </summary>
    public static class AfsCalendarQueries
    {
        /// <summary>Aantal gekoppelde agenda's van deze specialist.</summary>
        public static async Task<int> CountLinksAsync(string specialist) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT COUNT(*) FROM Specialists WHERE Id = @s AND EncryptedIcalUrl IS NOT NULL",
                new SqlParameter("@s", await AfsAppointmentsQueries.SpecialistIdAsync(specialist)))).Rows[0][0]);

        /// <summary>Aantal gesynchroniseerde agenda-items (ExternalCalendarEvent) van deze specialist.</summary>
        public static async Task<int> CountEventsAsync(string specialist) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT COUNT(*) FROM ExternalCalendarEvents WHERE SpecialistId = @s",
                new SqlParameter("@s", await AfsAppointmentsQueries.SpecialistIdAsync(specialist)))).Rows[0][0]);

        /// <summary>True wanneer de specialist een LastSyncedUtc heeft.</summary>
        public static async Task<bool> IsSyncedAsync(string specialist) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT COUNT(*) FROM Specialists WHERE Id = @s AND LastSyncedUtc IS NOT NULL",
                new SqlParameter("@s", await AfsAppointmentsQueries.SpecialistIdAsync(specialist)))).Rows[0][0]) > 0;
    }
}
