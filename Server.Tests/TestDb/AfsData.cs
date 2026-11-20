using Microsoft.Data.SqlClient;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Server.Tests.TestDb;
using Server.Tests.Utils;

namespace Server.Tests.Helpers
{
    /// <summary>
    /// Arrange/Assert via de database. LET OP: tabel- en kolomnamen zijn aannames (TDD-startpunt);
    /// pas ze aan naar het echte schema. De seed data bevat de patiënten, specialisten en gebruikers.
    /// </summary>
    public static class AfsData
    {
        public static async Task<int> PatientIdAsync(string name) =>
            Convert.ToInt32((await TestDatabase.QueryAsync("SELECT Id FROM Patients WHERE Name = @n", new SqlParameter("@n", name))).Rows[0][0]);

        public static async Task<int> SpecialistIdAsync(string name) =>
            Convert.ToInt32((await TestDatabase.QueryAsync("SELECT Id FROM Specialists WHERE Name = @n", new SqlParameter("@n", name))).Rows[0][0]);

        /// <summary>Zorgt voor precies één verwijzing met status "Open" voor de patiënt.</summary>
        public static async Task<int> CreateOpenReferralAsync(string patient, string specialism)
        {
            var patientId = await PatientIdAsync(patient);
            await TestDatabase.ExecuteAsync("DELETE FROM Referrals WHERE PatientId = @p", new SqlParameter("@p", patientId));
            var t = await TestDatabase.QueryAsync(
                "INSERT INTO Referrals (PatientId, Specialism, Status) OUTPUT INSERTED.Id VALUES (@p, @s, 'Open')",
                new SqlParameter("@p", patientId), new SqlParameter("@s", specialism));
            return Convert.ToInt32(t.Rows[0][0]);
        }

        public static async Task DeleteReferralsAsync(string patient) =>
            await TestDatabase.ExecuteAsync("DELETE FROM Referrals WHERE PatientId = @p", new SqlParameter("@p", await PatientIdAsync(patient)));

        public static async Task<int> CreateAppointmentAsync(string patient, string specialist, DateTime start, int referralId)
        {
            var t = await TestDatabase.QueryAsync(
                "INSERT INTO Appointments (PatientId, SpecialistId, ReferralId, Start) OUTPUT INSERTED.Id VALUES (@p, @s, @r, @st)",
                new SqlParameter("@p", await PatientIdAsync(patient)), new SqlParameter("@s", await SpecialistIdAsync(specialist)),
                new SqlParameter("@r", referralId), new SqlParameter("@st", start));
            await TestDatabase.ExecuteAsync("UPDATE Referrals SET Status = 'Ingepland' WHERE Id = @r", new SqlParameter("@r", referralId));
            return Convert.ToInt32(t.Rows[0][0]);
        }

        public static async Task SetUnavailableAsync(string specialist, DateTime date) =>
            await TestDatabase.ExecuteAsync("DELETE FROM Availabilities WHERE SpecialistId = @s AND CAST(Start AS date) = @d",
                new SqlParameter("@s", await SpecialistIdAsync(specialist)), new SqlParameter("@d", date.Date));

        public static async Task ResetAppointmentsAsync(params int[] referralIds)
        {
            await TestDatabase.ExecuteAsync("DELETE FROM Appointments");
            foreach (var id in referralIds)
                await TestDatabase.ExecuteAsync("UPDATE Referrals SET Status = 'Open' WHERE Id = @r", new SqlParameter("@r", id));
        }

        public static async Task<int> CountAppointmentsAsync(string specialist, DateTime start) =>
            Convert.ToInt32((await TestDatabase.QueryAsync("SELECT COUNT(*) FROM Appointments WHERE SpecialistId = @s AND Start = @st",
                new SqlParameter("@s", await SpecialistIdAsync(specialist)), new SqlParameter("@st", start))).Rows[0][0]);

        public static async Task<int> CountAppointmentsOfPatientAsync(string patient) =>
            Convert.ToInt32((await TestDatabase.QueryAsync("SELECT COUNT(*) FROM Appointments WHERE PatientId = @p",
                new SqlParameter("@p", await PatientIdAsync(patient)))).Rows[0][0]);

        public static async Task<string> ReferralStatusAsync(int referralId) =>
            (await TestDatabase.QueryAsync("SELECT Status FROM Referrals WHERE Id = @r", new SqlParameter("@r", referralId))).Rows[0][0].ToString()!;
    }

    /// <summary>
    /// Stuurt een request met de sessie van de ingelogde browser (fetch in de pagina), dus zonder de interface te gebruiken.
    /// Aanname: de API bereikbaar op hetzelfde domein onder /api en authenticatie via cookie.
    /// Is het een Bearer-token in localStorage, pas dan Post() aan.
    /// </summary>
    public static class BrowserApi
    {
        public static long PostAppointment(IWebDriver driver, int referralId, int specialistId, DateTime start)
        {
            var js = (IJavaScriptExecutor)driver;
            var body = $"{{\"referralId\":{referralId},\"specialistId\":{specialistId},\"start\":\"{start:yyyy-MM-ddTHH:mm:ss}\"}}";
            var result = js.ExecuteAsyncScript(@"
                const done = arguments[arguments.length - 1];
                fetch('/api/appointments', { method: 'POST', credentials: 'include',
                    headers: { 'Content-Type': 'application/json' }, body: arguments[0] })
                  .then(r => done(r.status)).catch(() => done(-1));", body);
            return Convert.ToInt64(result);
        }
    }

    /// <summary>Tweede (of derde) Chrome-browser voor tests met meerdere gelijktijdige sessies.</summary>
    public static class ExtraBrowser
    {
        public static IWebDriver Start()
        {
            var options = new ChromeOptions { AcceptInsecureCertificates = true };
            options.AddArgument("--no-proxy-server");
            options.AddArgument("--window-size=1920,1080");
            if (TestSettings.Headless) options.AddArgument("--headless=new");
            return new ChromeDriver(options);
        }
    }
}