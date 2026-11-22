using Microsoft.Data.SqlClient;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Server.Models.Enums;
using Server.Tests.TestDb;
using Server.Tests.Utils;

namespace Server.Tests.Helpers
{
    /// <summary>
    /// Arrange/Assert via de database. Namen uit de tests zijn de FullName van de User van de patiënt of specialist,
    /// omdat Patient en Specialist zelf geen naam hebben (die staat via UserId op User).
    /// Een afspraak maakt zelf een verwijzing aan; de helpers hier doen dat ook voor bestaande afspraken.
    /// </summary>
    public static class AfsData
    {
        private static SqlParameter NameParam(string name) => new("@u", name);

        public static async Task<int> PatientIdAsync(string name) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT p.Id FROM Patients p JOIN Users u ON u.Id = p.UserId WHERE u.FullName = @u",
                NameParam(name))).Rows[0][0]);

        public static async Task<int> SpecialistIdAsync(string name) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT s.Id FROM Specialists s JOIN Users u ON u.Id = s.UserId WHERE u.FullName = @u",
                NameParam(name))).Rows[0][0]);

        /// <summary>
        /// Maakt een bestaande afspraak (KNO, behandelcode KNO002 = 60 minuten) met een bijbehorende
        /// verwijzing met status "Ingepland", zoals de applicatie dat zelf zou doen.
        /// </summary>
        public static async Task<int> CreateAppointmentAsync(string patient, string specialist, DateTime start)
        {
            var t = await TestDatabase.QueryAsync(@"
                DECLARE @ref TABLE (Id int);

                INSERT INTO Referrals (PatientId, GeneralPractitionerId, Specialism, Reason, Questionnaire, Urgency, TreatmentCode, Status, CreatedAt)
                OUTPUT INSERTED.Id INTO @ref
                SELECT p.Id, p.GeneralPractitionerId, @spec, 'Testverwijzing', '', 'Regulier', 'KNO002', 'Ingepland', SYSUTCDATETIME()
                FROM Patients p WHERE p.Id = @p;

                INSERT INTO Appointments (PatientId, SpecialistId, ReferralId, StartTime, EndTime, Status)
                OUTPUT INSERTED.Id
                SELECT @p, @s, r.Id, @st, DATEADD(MINUTE, tc.DurationInQuarters * 15, @st), @status
                FROM @ref r CROSS JOIN TreatmentCodes tc
                WHERE tc.Code = 'KNO002';",
                new SqlParameter("@p", await PatientIdAsync(patient)),
                new SqlParameter("@s", await SpecialistIdAsync(specialist)),
                new SqlParameter("@st", start),
                new SqlParameter("@spec", (int)Specialism.KNO),
                new SqlParameter("@status", (int)AppointmentStatus.Ingepland));
            return Convert.ToInt32(t.Rows[0][0]);
        }

        public static async Task SetUnavailableAsync(string specialist, DateTime date) =>
            await TestDatabase.ExecuteAsync(
                "DELETE FROM Availabilities WHERE SpecialistId = @s AND CAST(StartTime AS date) = @d",
                new SqlParameter("@s", await SpecialistIdAsync(specialist)),
                new SqlParameter("@d", date.Date));

        /// <summary>
        /// Verwijdert alle afspraken en de verwijzingen die door een afspraak zijn aangemaakt (status "Ingepland").
        /// De open verwijzingen uit de seed blijven bestaan.
        /// </summary>
        public static async Task ResetAppointmentsAsync() =>
            await TestDatabase.ExecuteAsync(@"
                DELETE FROM Appointments;
                DELETE FROM Referrals WHERE Status = 'Ingepland';");

        public static async Task<int> CountAppointmentsAsync(string specialist, DateTime start) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT COUNT(*) FROM Appointments WHERE SpecialistId = @s AND StartTime = @st",
                new SqlParameter("@s", await SpecialistIdAsync(specialist)),
                new SqlParameter("@st", start))).Rows[0][0]);

        public static async Task<int> CountAppointmentsOfPatientAsync(string patient) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT COUNT(*) FROM Appointments WHERE PatientId = @p",
                new SqlParameter("@p", await PatientIdAsync(patient)))).Rows[0][0]);

        public static async Task<int> CountReferralsOfPatientAsync(string patient) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT COUNT(*) FROM Referrals WHERE PatientId = @p",
                new SqlParameter("@p", await PatientIdAsync(patient)))).Rows[0][0]);

        /// <summary>Alle verwijzingen, om te controleren dat er geen wees-verwijzing is achtergebleven.</summary>
        public static async Task<int> CountReferralsAsync() =>
            Convert.ToInt32((await TestDatabase.QueryAsync("SELECT COUNT(*) FROM Referrals")).Rows[0][0]);

        /// <summary>De verwijzing die bij de afspraak van deze specialist op dit tijdstip hoort.</summary>
        public static async Task<(string Status, string TreatmentCode, int Specialism)> AppointmentReferralAsync(
            string specialist, DateTime start)
        {
            var t = await TestDatabase.QueryAsync(@"
                SELECT r.Status, r.TreatmentCode, r.Specialism
                FROM Appointments a JOIN Referrals r ON r.Id = a.ReferralId
                WHERE a.SpecialistId = @s AND a.StartTime = @st",
                new SqlParameter("@s", await SpecialistIdAsync(specialist)),
                new SqlParameter("@st", start));

            var row = t.Rows[0];
            return (row["Status"].ToString()!, row["TreatmentCode"].ToString()!, Convert.ToInt32(row["Specialism"]));
        }
    }

    /// <summary>
    /// Stuurt een request met de sessie van de ingelogde browser (fetch in de pagina), dus zonder de interface te gebruiken.
    /// Aanname: de API is bereikbaar op hetzelfde domein onder /api en authenticatie gaat via een cookie.
    /// Is het een Bearer-token in localStorage, pas dan de fetch aan.
    /// </summary>
    public static class BrowserApi
    {
        /// <summary>Plant een afspraak (de API maakt zelf een verwijzing aan). Geeft de HTTP-statuscode (-1 bij een netwerkfout).</summary>
        public static long PostAppointment(
            IWebDriver driver,
            int patientId,
            int specialistId,
            DateTime start,
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
                generalPractitionerId,
                specialism = (int)specialism,
                preferredSpecialistId = specialistId,
                reason = "Testafspraak",
                urgency = "Regulier",
                treatmentCode
            });

            var result = js.ExecuteAsyncScript(@"
                const done = arguments[arguments.length - 1];
                fetch(arguments[1] + '/api/appointments', { method: 'POST', credentials: 'include',
                    headers: { 'Content-Type': 'application/json' }, body: arguments[0] })
                  .then(r => done(r.status)).catch(() => done(-1));", body, TestSettings.ApiUrl);
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