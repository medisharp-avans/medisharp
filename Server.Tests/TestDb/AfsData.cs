using Microsoft.Data.SqlClient;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Server.Models.Enums;
using Server.Tests.TestDb;
using Server.Tests.Utils;

namespace Server.Tests.Helpers
{
    /// <summary>
    /// Arrange/Assert via de database. Namen uit de tests worden gekoppeld aan de Username van de seed-data,
    /// omdat Patient en Specialist zelf geen naam hebben (die staat via UserId op User).
    /// </summary>
    public static class AfsData
    {
        private static readonly Dictionary<string, string> Usernames = new()
        {
            ["Jan Jansen"] = "jan.jansen",
            ["Piet Pietersen"] = "piet.pietersen",
            ["Anna de Boer"] = "anna.deboer",
            ["Dr. De Vries"] = "dr.devries",
            ["Dr. Bakker"] = "dr.bakker",
            ["Dr. Smit"] = "dr.smit"
        };

        private static SqlParameter UsernameParam(string name) =>
            new("@u", Usernames.TryGetValue(name, out var username)
                ? username
                : throw new ArgumentException($"'{name}' staat niet in de seed-data.", nameof(name)));

        public static async Task<int> PatientIdAsync(string name) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT p.Id FROM Patients p JOIN Users u ON u.Id = p.UserId WHERE u.Username = @u",
                UsernameParam(name))).Rows[0][0]);

        public static async Task<int> SpecialistIdAsync(string name) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT s.Id FROM Specialists s JOIN Users u ON u.Id = s.UserId WHERE u.Username = @u",
                UsernameParam(name))).Rows[0][0]);

        /// <summary>Zorgt voor precies één verwijzing met status "Open" voor de patiënt.</summary>
        public static async Task<int> CreateOpenReferralAsync(string patient, string specialism)
        {
            var patientId = await PatientIdAsync(patient);
            await DeleteReferralsAsync(patient);

            // Specialism is een enum (int) in Referrals, maar een string in TreatmentCodes.
            // De behandelcode wordt gekozen uit de seed-data op basis van het specialisme.
            var specialismValue = (int)Enum.Parse<Specialism>(specialism);
            var t = await TestDatabase.QueryAsync(@"
                INSERT INTO Referrals (PatientId, GeneralPractitionerId, Specialism, Reason, Questionnaire, Urgency, TreatmentCode, Status, CreatedAt)
                OUTPUT INSERTED.Id
                SELECT p.Id, p.GeneralPractitionerId, @s, 'Testverwijzing', '', 'Regulier',
                       (SELECT TOP 1 Code FROM TreatmentCodes WHERE Specialism = @sn ORDER BY Code),
                       'Open', SYSUTCDATETIME()
                FROM Patients p WHERE p.Id = @p",
                new SqlParameter("@p", patientId),
                new SqlParameter("@s", specialismValue),
                new SqlParameter("@sn", specialism));
            return Convert.ToInt32(t.Rows[0][0]);
        }

        /// <summary>Verwijdert de verwijzingen van de patiënt, en eerst de afspraken die eraan hangen (FK).</summary>
        public static async Task DeleteReferralsAsync(string patient)
        {
            var p = new SqlParameter("@p", await PatientIdAsync(patient));
            await TestDatabase.ExecuteAsync(@"
                DELETE FROM Appointments WHERE ReferralId IN (SELECT Id FROM Referrals WHERE PatientId = @p);
                DELETE FROM Referrals WHERE PatientId = @p;", p);
        }

        /// <summary>Maakt een afspraak (eindtijd volgt uit de behandelcode van de verwijzing) en zet de verwijzing op "Ingepland".</summary>
        public static async Task<int> CreateAppointmentAsync(string patient, string specialist, DateTime start, int referralId)
        {
            var t = await TestDatabase.QueryAsync(@"
                INSERT INTO Appointments (PatientId, SpecialistId, ReferralId, StartTime, EndTime, Status)
                OUTPUT INSERTED.Id
                SELECT @p, @s, r.Id, @st, DATEADD(MINUTE, tc.DurationInQuarters * 15, @st), @status
                FROM Referrals r
                JOIN TreatmentCodes tc ON tc.Code = r.TreatmentCode
                WHERE r.Id = @r",
                new SqlParameter("@p", await PatientIdAsync(patient)),
                new SqlParameter("@s", await SpecialistIdAsync(specialist)),
                new SqlParameter("@r", referralId),
                new SqlParameter("@st", start),
                new SqlParameter("@status", (int)AppointmentStatus.Ingepland));

            await TestDatabase.ExecuteAsync("UPDATE Referrals SET Status = 'Ingepland' WHERE Id = @r",
                new SqlParameter("@r", referralId));
            return Convert.ToInt32(t.Rows[0][0]);
        }

        public static async Task SetUnavailableAsync(string specialist, DateTime date) =>
            await TestDatabase.ExecuteAsync(
                "DELETE FROM Availabilities WHERE SpecialistId = @s AND CAST(StartTime AS date) = @d",
                new SqlParameter("@s", await SpecialistIdAsync(specialist)),
                new SqlParameter("@d", date.Date));

        public static async Task ResetAppointmentsAsync(params int[] referralIds)
        {
            await TestDatabase.ExecuteAsync("DELETE FROM Appointments");
            foreach (var id in referralIds)
                await TestDatabase.ExecuteAsync("UPDATE Referrals SET Status = 'Open' WHERE Id = @r", new SqlParameter("@r", id));
        }

        public static async Task<int> CountAppointmentsAsync(string specialist, DateTime start) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT COUNT(*) FROM Appointments WHERE SpecialistId = @s AND StartTime = @st",
                new SqlParameter("@s", await SpecialistIdAsync(specialist)),
                new SqlParameter("@st", start))).Rows[0][0]);

        public static async Task<int> CountAppointmentsOfPatientAsync(string patient) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT COUNT(*) FROM Appointments WHERE PatientId = @p",
                new SqlParameter("@p", await PatientIdAsync(patient)))).Rows[0][0]);

        public static async Task<int> CountAppointmentsWithoutReferralAsync(string patient) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT COUNT(*) FROM Appointments WHERE PatientId = @p AND ReferralId IS NULL",
                new SqlParameter("@p", await PatientIdAsync(patient)))).Rows[0][0]);

        public static async Task<string> ReferralStatusAsync(int referralId) =>
            (await TestDatabase.QueryAsync("SELECT Status FROM Referrals WHERE Id = @r",
                new SqlParameter("@r", referralId))).Rows[0][0].ToString()!;
    }

    /// <summary>
    /// Stuurt een request met de sessie van de ingelogde browser (fetch in de pagina), dus zonder de interface te gebruiken.
    /// Aanname: de API is bereikbaar op hetzelfde domein onder /api en authenticatie gaat via een cookie.
    /// Is het een Bearer-token in localStorage, pas dan de fetch aan.
    /// </summary>
    public static class BrowserApi
    {
        /// <summary>Geeft de HTTP-statuscode terug (-1 bij een netwerkfout).</summary>
        public static long PostAppointment(
            IWebDriver driver,
            int patientId,
            int specialistId,
            DateTime start,
            int? referralId = null,
            string treatmentCode = "KNO002",
            int generalPractitionerId = 1,
            Specialism specialism = Specialism.KNO)
        {
            var js = (IJavaScriptExecutor)driver;
            var body = System.Text.Json.JsonSerializer.Serialize(new
            {
                patientId,
                specialistId,
                startTime = start.ToString("yyyy-MM-ddTHH:mm:ss"),
                referralId,
                generalPractitionerId,
                specialism = (int)specialism,
                reason = "Testafspraak",
                urgency = "Regulier",
                treatmentCode
            });

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