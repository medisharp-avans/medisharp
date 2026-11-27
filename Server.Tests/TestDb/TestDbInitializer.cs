using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Tests.TestDb
{
    /// <summary>
    /// Fills the empty test database with the fixed test data of the test cases.
    /// Runs before every database and end-to-end test, right after the tables were emptied.
    /// </summary>
    public static class TestDbInitializer
    {
        /// <summary>The password of every test user.</summary>
        public const string Password = "Test123!";

        // Hashing is slow on purpose, so the hash is made once for the whole test run.
        private static readonly Lazy<string> _passwordHash = new(() => new PasswordHasher<User>().HashPassword(new User(), Password));

        /// <summary>
        /// Inserts the seed data. Only use fictitious data here (DoD 19).
        ///
        /// Users (FullName is the name the tests use):
        ///   1 Jan Jansen (patient)         2 Dr. Visser (huisarts)       3 Dr. De Vries (specialist, KNO)
        ///   4 Piet Pietersen (patient)     5 Anna de Boer (patient)      6 Dr. Bakker (specialist, KNO)
        ///   7 Dr. Smit (specialist, Cardiologie)   8 and 9 Medewerker A and B   10 Beheerder Een
        /// Patient, specialist and GP ids: patients 1-3, specialists 1-3 (same order as the users), GP 1.
        /// Referrals 1 and 2 belong to patient 1. Availability: 08:00-17:00 on every day of January 2027.
        /// </summary>
        public static async Task SeedAsync(DbContext context)
        {
            // Fixed ids keep the test cases readable ("patient 1"). Every user logs in with Password.
            // {0} is the password hash; ExecuteSqlRawAsync passes it as a SQL parameter.
            await context.Database.ExecuteSqlRawAsync(@"
                SET IDENTITY_INSERT Users ON;
                INSERT INTO Users (Id, SurName, LastName, FullName, PasswordHash, Email, Role, IsActive, IsLockedOut) VALUES
                    (1,  'Jan',        'Jansen',    'Jan Jansen',      {0}, 'jan.jansen@test.nl',      0, 1, 0),
                    (2,  'Dr.',        'Visser',    'Dr. Visser',      {0}, 'visser@test.nl',          1, 1, 0),
                    (3,  'Dr.',        'De Vries',  'Dr. De Vries',    {0}, 'devries@test.nl',         2, 1, 0),
                    (4,  'Piet',       'Pietersen', 'Piet Pietersen',  {0}, 'piet.pietersen@test.nl',  0, 1, 0),
                    (5,  'Anna',       'de Boer',   'Anna de Boer',    {0}, 'anna.deboer@test.nl',     0, 1, 0),
                    (6,  'Dr.',        'Bakker',    'Dr. Bakker',      {0}, 'bakker@test.nl',          2, 1, 0),
                    (7,  'Dr.',        'Smit',      'Dr. Smit',        {0}, 'smit@test.nl',            2, 1, 0),
                    (8,  'Medewerker', 'A',         'Medewerker A',    {0}, 'medewerker.a@test.nl',    3, 1, 0),
                    (9,  'Medewerker', 'B',         'Medewerker B',    {0}, 'medewerker.b@test.nl',    3, 1, 0),
                    (10, 'Beheerder',  'Een',       'Beheerder Een',   {0}, 'beheerder1@test.nl',      4, 1, 0);
                SET IDENTITY_INSERT Users OFF;

                SET IDENTITY_INSERT GeneralPractitioners ON;
                INSERT INTO GeneralPractitioners (Id, UserId) VALUES (1, 2);
                SET IDENTITY_INSERT GeneralPractitioners OFF;

                -- Specialism: 4 = KNO, 0 = Cardiologie
                SET IDENTITY_INSERT Specialists ON;
                INSERT INTO Specialists (Id, UserId, Specialism) VALUES
                    (1, 3, 4),   -- Dr. De Vries (KNO)
                    (2, 6, 4),   -- Dr. Bakker   (KNO)
                    (3, 7, 0);   -- Dr. Smit     (Cardiologie)
                SET IDENTITY_INSERT Specialists OFF;

                SET IDENTITY_INSERT Patients ON;
                INSERT INTO Patients (Id, UserId, Bsn, PhoneNumber, Address, City, GeneralPractitionerId, HasInsurance) VALUES
                    (1, 1, '111222333', '0612345678', 'Teststraat 1', 'Utrecht', 1, 1),
                    (2, 4, '111222334', '0612345679', 'Teststraat 2', 'Utrecht', 1, 1),
                    (3, 5, '111222335', '0612345680', 'Teststraat 3', 'Utrecht', 1, 1);
                SET IDENTITY_INSERT Patients OFF;

                -- Code is the primary key (no identity). KNO002 lasts 4 quarters = 60 minutes.
                INSERT INTO TreatmentCodes (Code, Description, Specialism, DurationInQuarters, Cost) VALUES
                    ('ABC123', 'Consult cardiologie',    'Cardiologie',  2, 100.00),
                    ('DER001', 'Consult dermatologie',   'Dermatologie', 1,  60.00),
                    ('KNO002', 'Uitgebreid consult KNO', 'KNO',          4, 180.00);

                SET IDENTITY_INSERT Referrals ON;
                INSERT INTO Referrals (Id, PatientId, GeneralPractitionerId, Specialism, PreferredSpecialistId, Reason, Questionnaire, Urgency, TreatmentCode, Status, CreatedAt) VALUES
                    (1, 1, 1, 0, NULL, 'Pijn op de borst', '', 'Regulier', 'ABC123', 'Open', '2026-01-01T09:00:00'),
                    (2, 1, 1, 1, NULL, 'Huiduitslag',      '', 'Regulier', 'DER001', 'Open', '2026-01-02T09:00:00');
                SET IDENTITY_INSERT Referrals OFF;

                -- Availability: every specialist 08:00-17:00 on every day of January 2027
                ;WITH D AS (
                    SELECT CAST('2027-01-01' AS date) AS Dag
                    UNION ALL
                    SELECT DATEADD(DAY, 1, Dag) FROM D WHERE Dag < '2027-01-31'
                )
                INSERT INTO Availabilities (SpecialistId, StartTime, EndTime)
                SELECT s.Id,
                       DATEADD(HOUR, 8,  CAST(D.Dag AS datetime2)),
                       DATEADD(HOUR, 17, CAST(D.Dag AS datetime2))
                FROM D CROSS JOIN Specialists s;", _passwordHash.Value);

            context.ChangeTracker.Clear();
        }
    }
}