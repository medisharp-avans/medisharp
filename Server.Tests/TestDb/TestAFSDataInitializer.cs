
using Server.Models;
using Server.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Server.Tests.TestDb
{
    /// <summary>
    /// Fills the empty test database with the fixed test data of the test cases.
    /// Runs before every database and end-to-end test, right after the tables were emptied.
    /// </summary>
    public static class TestAFSDataInitializer
    {
        public static async Task SeedAsync(DbContext context)
        {
            // Users
            var patientUser1 = new User
            {
                Username = "jan.jansen",
                SurName = "Jan",
                LastName = "Jansen",
                FullName = "Jan Jansen",
                PasswordHash = "fake-hash-niet-echt",
                Email = "jan.jansen@test.nl",
                Role = UserRole.Patient
            };

            var gpUser = new User
            {
                Username = "huisarts.visser",
                SurName = "Dr.",
                LastName = "Visser",
                FullName = "Dr. Visser",
                PasswordHash = "fake-hash-niet-echt",
                Email = "visser@test.nl",
                Role = UserRole.Huisarts
            };

            var specialistUser1 = new User
            {
                Username = "dr.devries",
                SurName = "Dr.",
                LastName = "De Vries",
                FullName = "Dr. De Vries",
                PasswordHash = "fake-hash-niet-echt",
                Email = "devries@test.nl",
                Role = UserRole.Specialist
            };

            var patientUser2 = new User
            {
                Username = "piet.pietersen",
                SurName = "Piet",
                LastName = "Pietersen",
                FullName = "Piet Pietersen",
                PasswordHash = "fake-hash-niet-echt",
                Email = "piet.pietersen@test.nl",
                Role = UserRole.Patient
            };

            var patientUser3 = new User
            {
                Username = "anna.deboer",
                SurName = "Anna",
                LastName = "de Boer",
                FullName = "Anna de Boer",
                PasswordHash = "fake-hash-niet-echt",
                Email = "anna.deboer@test.nl",
                Role = UserRole.Patient
            };

            var specialistUser2 = new User
            {
                Username = "dr.bakker",
                SurName = "Dr.",
                LastName = "Bakker",
                FullName = "Dr. Bakker",
                PasswordHash = "fake-hash-niet-echt",
                Email = "bakker@test.nl",
                Role = UserRole.Specialist
            };

            var specialistUser3 = new User
            {
                Username = "dr.smit",
                SurName = "Dr.",
                LastName = "Smit",
                FullName = "Dr. Smit",
                PasswordHash = "fake-hash-niet-echt",
                Email = "smit@test.nl",
                Role = UserRole.Specialist
            };

            var employeeUser1 = new User
            {
                Username = "medewerker.a",
                SurName = "Medewerker",
                LastName = "A",
                FullName = "Medewerker A",
                PasswordHash = "fake-hash-niet-echt",
                Email = "medewerker.a@test.nl",
                Role = UserRole.Ziekenhuismedewerker
            };

            var employeeUser2 = new User
            {
                Username = "medewerker.b",
                SurName = "Medewerker",
                LastName = "B",
                FullName = "Medewerker B",
                PasswordHash = "fake-hash-niet-echt",
                Email = "medewerker.b@test.nl",
                Role = UserRole.Ziekenhuismedewerker
            };

            var adminUser = new User
            {
                Username = "beheerder1",
                SurName = "Beheerder",
                LastName = "Een",
                FullName = "Beheerder Een",
                PasswordHash = "fake-hash-niet-echt",
                Email = "beheerder1@test.nl",
                Role = UserRole.Systeembeheerder
            };

            context.Set<User>().AddRange(
                patientUser1,
                gpUser,
                specialistUser1,
                patientUser2,
                patientUser3,
                specialistUser2,
                specialistUser3,
                employeeUser1,
                employeeUser2,
                adminUser
            );

            await context.SaveChangesAsync();

            // General practitioner
            var generalPractitioner = new GeneralPractitioner
            {
                User = gpUser
            };

            context.Set<GeneralPractitioner>().Add(generalPractitioner);

            // Specialists
            var specialist1 = new Specialist
            {
                User = specialistUser1,
                Specialism = Specialism.KNO
            };

            var specialist2 = new Specialist
            {
                User = specialistUser2,
                Specialism = Specialism.KNO
            };

            var specialist3 = new Specialist
            {
                User = specialistUser3,
                Specialism = Specialism.Cardiologie
            };

            context.Set<Specialist>().AddRange(
                specialist1,
                specialist2,
                specialist3
            );

            await context.SaveChangesAsync();

            // Patients
            var patient1 = new Patient
            {
                User = patientUser1,
                Bsn = "111222333",
                PhoneNumber = "0612345678",
                Address = "Teststraat 1",
                City = "Utrecht",
                GeneralPractitioner = generalPractitioner,
                HasInsurance = true
            };

            var patient2 = new Patient
            {
                User = patientUser2,
                Bsn = "111222334",
                PhoneNumber = "0612345679",
                Address = "Teststraat 2",
                City = "Utrecht",
                GeneralPractitioner = generalPractitioner,
                HasInsurance = true
            };

            var patient3 = new Patient
            {
                User = patientUser3,
                Bsn = "111222335",
                PhoneNumber = "0612345680",
                Address = "Teststraat 3",
                City = "Utrecht",
                GeneralPractitioner = generalPractitioner,
                HasInsurance = true
            };

            context.Set<Patient>().AddRange(patient1, patient2, patient3);

            // Treatment codes
            var cardiologyCode = new TreatmentCode
            {
                Code = "ABC123",
                Description = "Consult cardiologie",
                Specialism = Specialism.Cardiologie,
                DurationInQuarters = 2,
                Cost = 100.00m
            };

            var dermatologyCode = new TreatmentCode
            {
                Code = "DER001",
                Description = "Consult dermatologie",
                Specialism = Specialism.Dermatologie,
                DurationInQuarters = 1,
                Cost = 60.00m
            };

            var knoCode = new TreatmentCode
            {
                Code = "KNO002",
                Description = "Uitgebreid consult KNO",
                Specialism = Specialism.KNO,
                DurationInQuarters = 4,
                Cost = 180.00m
            };

            context.Set<TreatmentCode>().AddRange(
                cardiologyCode,
                dermatologyCode,
                knoCode
            );

            await context.SaveChangesAsync();

            // Referrals
            var referral1 = new Referral
            {
                Patient = patient1,
                GeneralPractitioner = generalPractitioner,
                Specialism = Specialism.Cardiologie,
                Reason = "Pijn op de borst",
                Questionnaire = string.Empty,
                Urgency = "Regulier",
                TreatmentCode = cardiologyCode.Code,
                TreatmentCodeDetails = cardiologyCode,
                Status = "Open",
                CreatedAt = new DateTime(2026, 1, 1, 9, 0, 0)
            };

            var referral2 = new Referral
            {
                Patient = patient1,
                GeneralPractitioner = generalPractitioner,
                Specialism = Specialism.Dermatologie,
                Reason = "Huiduitslag",
                Questionnaire = string.Empty,
                Urgency = "Regulier",
                TreatmentCode = dermatologyCode.Code,
                TreatmentCodeDetails = dermatologyCode,
                Status = "Open",
                CreatedAt = new DateTime(2026, 1, 2, 9, 0, 0)
            };

            context.Set<Referral>().AddRange(referral1, referral2);

            await context.SaveChangesAsync();

            context.ChangeTracker.Clear();
        }
    }
}
