using Microsoft.Data.SqlClient;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Server.Models.Enums;
using Server.Tests.TestDb;
using Server.Tests.Utils;

namespace Server.Tests.TestDb.QueryHelpers
{
    public static class AfsAppointmentsQueries
    {
        private static readonly TimeZoneInfo DutchTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        private static SqlParameter NameParam(string name) => new("@u", name);

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

            return (
                row["Status"].ToString()!,
                row["TreatmentCode"].ToString()!,
                (int)Enum.Parse<Specialism>(row["Specialism"].ToString()!)
            );
        }


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
                new SqlParameter("@spec", Specialism.KNO.ToString()),
                new SqlParameter("@status", AppointmentStatus.Ingepland.ToString()));
            return Convert.ToInt32(t.Rows[0][0]);
        }

        public static async Task BlockSpecialistExternalAsync(string specialist, DateTime startLocal, DateTime endLocal)
        {
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified), DutchTimeZone);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified), DutchTimeZone);

            await TestDatabase.ExecuteAsync(@"
                INSERT INTO ExternalCalendarEvents (SpecialistId, ExternalUid, Title, StartUtc, EndUtc, IsAllDay)
                VALUES (@s, @uid, 'Privé blokkade', @st, @end, 0);",
                new SqlParameter("@s", await SpecialistIdAsync(specialist)),
                new SqlParameter("@uid", $"test-block-{Guid.NewGuid()}"),
                new SqlParameter("@st", startUtc),
                new SqlParameter("@end", endUtc));
        }

        public static async Task MakeOnlyAvailableAsync(string specialist, Specialism specialism, DateTime windowStartLocal, DateTime windowEndLocal)
        {
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(windowStartLocal, DateTimeKind.Unspecified), DutchTimeZone);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(windowEndLocal, DateTimeKind.Unspecified), DutchTimeZone);

            await TestDatabase.ExecuteAsync(@"
                DELETE e FROM ExternalCalendarEvents e
                JOIN Specialists s ON s.Id = e.SpecialistId
                WHERE s.Specialism = @spec;

                INSERT INTO ExternalCalendarEvents (SpecialistId, ExternalUid, Title, StartUtc, EndUtc, IsAllDay)
                SELECT s.Id, CONCAT('block-other-', s.Id, '-', NEWID()), 'Volgeboekt iCal', @st, @end, 0
                FROM Specialists s
                WHERE s.Specialism = @spec AND s.Id <> @s;",
                new SqlParameter("@spec", specialism.ToString()),
                new SqlParameter("@s", await SpecialistIdAsync(specialist)),
                new SqlParameter("@st", startUtc),
                new SqlParameter("@end", endUtc));
        }


        public static async Task BlockAllSpecialistsForWindowAsync(Specialism specialism, DateTime windowStartLocal, DateTime windowEndLocal)
        {
            var startUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(windowStartLocal, DateTimeKind.Unspecified), DutchTimeZone);
            var endUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(windowEndLocal, DateTimeKind.Unspecified), DutchTimeZone);

            await TestDatabase.ExecuteAsync(@"
                INSERT INTO ExternalCalendarEvents (SpecialistId, ExternalUid, Title, StartUtc, EndUtc, IsAllDay)
                SELECT s.Id, CONCAT('block-all-', s.Id, '-', NEWID()), 'Volgeboekt iCal', @st, @end, 0
                FROM Specialists s
                WHERE s.Specialism = @spec;",
                new SqlParameter("@spec", specialism.ToString()),
                new SqlParameter("@st", startUtc),
                new SqlParameter("@end", endUtc));
        }

        public static async Task ResetExternalEventsAsync() =>
            await TestDatabase.ExecuteAsync("DELETE FROM ExternalCalendarEvents;");

        public static async Task ResetAppointmentsAsync() =>
            await TestDatabase.ExecuteAsync(@"
                DELETE FROM Appointments;
                DELETE FROM Referrals WHERE Status = 'Ingepland';");

        public static async Task<int> PatientIdAsync(string name) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT p.Id FROM Patients p JOIN Users u ON u.Id = p.UserId WHERE u.FullName = @u",
                NameParam(name))).Rows[0][0]);

        public static async Task<int> SpecialistIdAsync(string name) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT s.Id FROM Specialists s JOIN Users u ON u.Id = s.UserId WHERE u.FullName = @u",
                NameParam(name))).Rows[0][0]);

        // ... in alle Count-methodes:
        public static async Task<int> CountAppointmentsAsync(string specialist, DateTime start) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT COUNT(*) FROM Appointments WHERE SpecialistId = @s AND StartTime = @st",
                new SqlParameter("@s", await SpecialistIdAsync(specialist)),
                new SqlParameter("@st", start))).Rows[0][0]);

        public static async Task<int> CountTotalAppointmentsOfSpecialistAsync(string specialist) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT COUNT(*) FROM Appointments WHERE SpecialistId = @s",
                new SqlParameter("@s", await SpecialistIdAsync(specialist)))).Rows[0][0]);

        public static async Task<int> CountAppointmentsOfPatientAsync(string patient) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT COUNT(*) FROM Appointments WHERE PatientId = @p",
                new SqlParameter("@p", await PatientIdAsync(patient)))).Rows[0][0]);

        public static async Task<int> CountReferralsOfPatientAsync(string patient) =>
            Convert.ToInt32((await TestDatabase.QueryAsync(
                "SELECT COUNT(*) FROM Referrals WHERE PatientId = @p",
                new SqlParameter("@p", await PatientIdAsync(patient)))).Rows[0][0]);

        public static async Task<int> CountReferralsAsync() =>
            Convert.ToInt32((await TestDatabase.QueryAsync("SELECT COUNT(*) FROM Referrals")).Rows[0][0]);
    }


        public static class BrowserApi
    {
        public static (long Status, string Body) PostAppointmentWithResponse(
            IWebDriver driver,
            int patientId,
            int specialistId,
            string treatmentCode = "KNO002",
            Specialism specialism = Specialism.KNO)
        {
            var js = (IJavaScriptExecutor)driver;

            var body = System.Text.Json.JsonSerializer.Serialize(new
            {
                patientId,
                specialism = (int)specialism,
                preferredSpecialistId = specialistId,
                reason = "Testafspraak",
                questionnaire = "",
                urgency = "Regulier",
                treatmentCode
            });

            var result = js.ExecuteAsyncScript(@"
                const done = arguments[arguments.length - 1];

                fetch(arguments[1] + '/api/appointments', {
                    method: 'POST',
                    credentials: 'include',
                    headers: { 'Content-Type': 'application/json' },
                    body: arguments[0]
                })
                .then(async response => {
                    const responseBody = await response.text();

                    done(JSON.stringify({
                        status: response.status,
                        body: responseBody
                    }));
                })
                .catch(error => {
                    done(JSON.stringify({
                        status: -1,
                        body: error.toString()
                    }));
                });
            ", body, TestSettings.ApiUrl);

            using var json = System.Text.Json.JsonDocument.Parse(
                Convert.ToString(result)!);

            return (
                json.RootElement.GetProperty("status").GetInt32(),
                json.RootElement.GetProperty("body").GetString() ?? ""
            );
        }
    }

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