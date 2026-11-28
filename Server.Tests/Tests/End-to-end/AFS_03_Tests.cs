using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using Server.Models.Enums;
using Server.Tests.PageModels.AppointmentPages;
using Server.Tests.TestDb;
using Server.Tests.TestDb.QueryHelpers;
using Server.Tests.Utils;

namespace Server.Tests.EndToEnd
{
    [TestClass]
    public class AFS_03_Tests : EndToEndTestBase
    {
        private const string DeVries = "Dr. De Vries";
        private const string Bakker = "Dr. Bakker";
        private const string Smit = "Dr. Smit";
        private const string Jan = "Jan Jansen";
        private const string Piet = "Piet Pietersen";
        private const string Anna = "Anna de Boer";

        private const string MsgNoSpecialist =
            "Geen beschikbare specialist gevonden binnen de termijn van 3 weken.";

        private static readonly TimeZoneInfo DutchTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        private static DateTime WindowStart =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, DutchTimeZone).Date.AddDays(1);

        private static DateTime WindowEnd =>
            WindowStart.AddDays(21);

        private static DateTime FirstSlot
        {
            get
            {
                var day = WindowStart;
                while (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                {
                    day = day.AddDays(1);
                }
                return day.AddHours(9);
            }
        }

        private static DateTime NextWorkdaySlot(DateTime currentSlot)
        {
            var day = currentSlot.Date.AddDays(1);
            while (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                day = day.AddDays(1);
            }
            return day.AddHours(9);
        }

        // =========================================================
        // Helpers
        // =========================================================
        protected override Func<DbContext, Task>? GetTestDataInitializer()
        {
            return TestAFSDataInitializer.SeedAsync;
        }

        [TestInitialize]
        public async Task ArrangeTestDataAsync()
        {
            await AfsAppointmentsQueries.MakeOnlyAvailableAsync(
                DeVries,
                Specialism.KNO,
                WindowStart,
                WindowEnd);
        }

        private static void LoginAs(IWebDriver driver, string username)
        {
            // new LoginPage(driver).LoginAs(username);
        }

        private Task<PlanAppointmentPage> OpenPlanningAsync(
            IWebDriver driver,
            string staffUser)
        {
            LoginAs(driver, staffUser);
            var plan = new PlanAppointmentPage(driver).Open();
            return Task.FromResult(plan);
        }

        private static void Book(
            PlanAppointmentPage plan,
            string patient,
            string? preferredSpecialist = DeVries)
        {
            plan.FillForm(
                patient,
                Specialism.KNO,
                preferredSpecialist);

            plan.Submit();
            plan.WaitForMessage();
        }

        private static async Task<(long Status, string Body)> PostAsync(
            IWebDriver driver,
            string patient,
            string specialist)
        {
            var patientId = await AfsAppointmentsQueries.PatientIdAsync(patient);
            var specialistId = await AfsAppointmentsQueries.SpecialistIdAsync(specialist);

            return BrowserApi.PostAppointmentWithResponse(
                driver,
                patientId,
                specialistId);
        }

        // =========================================================
        // AC-AFS-03_1 – Medewerker plant een afspraak
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_1_1")]
        public async Task TC_AFS_03_1_1_Medewerker_PlantAfspraak_AfspraakEnVerwijzingZijnOpgeslagen()
        {
            var referralsBefore =
                await AfsAppointmentsQueries.CountReferralsOfPatientAsync(Jan);

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a");

            Book(plan, Jan);

            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);
            Assert.AreEqual("patient", plan.CurrentStepId);
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot));
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsOfPatientAsync(Jan));
            Assert.AreEqual(referralsBefore + 1, await AfsAppointmentsQueries.CountReferralsOfPatientAsync(Jan));
            Assert.AreEqual("Ingepland", (await AfsAppointmentsQueries.AppointmentReferralAsync(DeVries, FirstSlot)).Status);
        }

        [TestMethod, TestCategory("TC-AFS-03_1_2")]
        public async Task TC_AFS_03_1_2_Medewerker_PlantTijdstipDirectNaBestaandeAfspraak_WordtOpgeslagen()
        {
            var secondSlot = FirstSlot.AddHours(1);

            await AfsAppointmentsQueries.CreateAppointmentAsync(
                Piet, DeVries, FirstSlot);

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a");

            Book(plan, Jan);

            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot));
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, secondSlot));
        }

        [TestMethod, TestCategory("TC-AFS-03_1_3")]
        [Ignore("Wacht op login: rol (huisarts/patiënt) kan nog niet worden aangenomen.")]
        public async Task TC_AFS_03_1_3_Huisarts_KanGeenAfspraakPlannen_BackendWeigert()
        {
            var referralsBefore = await AfsAppointmentsQueries.CountReferralsAsync();
            LoginAs(Driver, "huisarts.visser");

            var response = await PostAsync(Driver, Jan, DeVries);

            Assert.AreEqual(403, response.Status);
            Assert.AreEqual(0, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot));
            Assert.AreEqual(referralsBefore, await AfsAppointmentsQueries.CountReferralsAsync());
        }

        // =========================================================
        // AC-AFS-03_2 – Automatisch aanmaken van een verwijzing
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_2_1")]
        public async Task TC_AFS_03_2_1_Medewerker_PlantZonderBestaandeVerwijzing_VerwijzingWordtAangemaakt()
        {
            Assert.AreEqual(0, await AfsAppointmentsQueries.CountReferralsOfPatientAsync(Piet));

            var plan = await OpenPlanningAsync(Driver, "medewerker.a");
            Book(plan, Piet);

            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsOfPatientAsync(Piet));
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountReferralsOfPatientAsync(Piet));

            var referral = await AfsAppointmentsQueries.AppointmentReferralAsync(DeVries, FirstSlot);
            Assert.AreEqual("Ingepland", referral.Status);
            Assert.AreEqual((int)Specialism.KNO, referral.Specialism);
            Assert.AreEqual("KNO002", referral.TreatmentCode);
        }

        [TestMethod, TestCategory("TC-AFS-03_2_2")]
        public async Task TC_AFS_03_2_2_Medewerker_PlantVervolgafspraak_KrijgtEigenVerwijzing()
        {
            var earlier = FirstSlot.AddDays(-7);
            await AfsAppointmentsQueries.CreateAppointmentAsync(Jan, DeVries, earlier);

            var referralsBefore = await AfsAppointmentsQueries.CountReferralsOfPatientAsync(Jan);
            var plan = await OpenPlanningAsync(Driver, "medewerker.a");

            Book(plan, Jan);

            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);
            Assert.AreEqual(2, await AfsAppointmentsQueries.CountAppointmentsOfPatientAsync(Jan));
            Assert.AreEqual(referralsBefore + 1, await AfsAppointmentsQueries.CountReferralsOfPatientAsync(Jan));
            Assert.AreEqual("Ingepland", (await AfsAppointmentsQueries.AppointmentReferralAsync(DeVries, earlier)).Status);
            Assert.AreEqual("Ingepland", (await AfsAppointmentsQueries.AppointmentReferralAsync(DeVries, FirstSlot)).Status);
        }

        [TestMethod, TestCategory("TC-AFS-03_2_3")]
        public async Task TC_AFS_03_2_3_Backend_AccepteertAfspraakZonderVerwijzingEnMaaktErEenAan()
        {
            Driver.Navigate().GoToUrl(TestSettings.ClientUrl);

            var response = await PostAsync(Driver, Piet, DeVries);

            Assert.IsTrue(
                response.Status is >= 200 and < 300,
                $"Verwacht succesvolle boeking (2xx), kreeg {response.Status}.\nBackend response: {response.Body}");

            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsOfPatientAsync(Piet));
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountReferralsOfPatientAsync(Piet));
        }


        // =========================================================
        // AC-AFS-03_3 – Automatisch doorschuiven & opnieuw bevestigen
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_3_1")]
        public async Task TC_AFS_03_3_1_Medewerker_BoektTerwijlEersteSlotNetIsGeboekt_SchuiftDoorNaarVolgendSlot()
        {
            using var driverB = ExtraBrowser.Start();

            var planA = await OpenPlanningAsync(Driver, "medewerker.a");
            var planB = await OpenPlanningAsync(driverB, "medewerker.b");

            planA.FillForm(Jan, Specialism.KNO, DeVries);
            planB.FillForm(Piet, Specialism.KNO, DeVries);

            // Medewerker B bevestigt als eerste -> krijgt FirstSlot (09:00)
            planB.Submit();
            planB.WaitForMessage();

            Assert.IsTrue(planB.HasSuccessMessage, planB.Message);
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot));

            // Medewerker A bevestigt daarna -> het algoritme kiest automatisch het aansluitende slot (10:00)
            planA.Submit();
            planA.WaitForMessage();

            Assert.IsTrue(planA.HasSuccessMessage, planA.Message);
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot.AddHours(1)));
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsOfPatientAsync(Jan));
        }

        [TestMethod, TestCategory("TC-AFS-03_3_2")]
        public async Task TC_AFS_03_3_2_Medewerker_BevestigtOpnieuwNaMislukteBoeking_WanneerTijdstipVrijIs()
        {
            // Blokkeer alle KNO-specialisten voor de gehele 3-wekentermijn
            await AfsAppointmentsQueries.BlockAllSpecialistsForWindowAsync(
                Specialism.KNO, WindowStart, WindowEnd);

            var plan = await OpenPlanningAsync(Driver, "medewerker.a");
            plan.FillForm(Jan, Specialism.KNO, DeVries);

            // Act 1: eerste poging mislukt
            plan.Submit();
            plan.WaitForMessage();

            Assert.IsTrue(plan.HasErrorMessage, plan.Message);
            Assert.AreEqual("review", plan.CurrentStepId);

            // Act 2: de agenda's komen vrij en de medewerker bevestigt opnieuw
            await AfsAppointmentsQueries.ResetExternalEventsAsync();
            await AfsAppointmentsQueries.MakeOnlyAvailableAsync(
                DeVries, Specialism.KNO, WindowStart, WindowEnd);

            plan.Submit();
            plan.WaitForSuccessMessage();

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot));
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsOfPatientAsync(Jan));
        }

        // =========================================================
        // AC-AFS-03_4 – Het handmatige afsprakenformulier
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_4_1")]
        public async Task TC_AFS_03_4_1_Afsprakenpagina_ToontAlleBenodigdeFormuliervelden()
        {
            var plan = await OpenPlanningAsync(Driver, "medewerker.a");

            Assert.AreEqual("patient", plan.CurrentStepId);
            Assert.IsTrue(plan.HasField("patient-id"));
            Assert.IsTrue(plan.HasField("specialism"));

            plan.FillPatientStep(Jan, Specialism.KNO).NextStep("care");
            Assert.IsTrue(plan.HasField("specialist-id"));
            Assert.IsTrue(plan.HasField("treatment-code"));

            plan.FillCareStep().NextStep("details");
            Assert.IsTrue(plan.HasField("reason"));
            Assert.IsTrue(plan.HasField("questionnaire"));
            Assert.IsTrue(plan.HasField("urgency"));

            plan.FillDetailsStep().NextStep("review");
            Assert.IsTrue(plan.HasField("submit"));
        }

        // =========================================================
        // AC-AFS-03_6 – Specialist heeft al een afspraak
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_6_1")]
        public async Task TC_AFS_03_6_1_Medewerker_PlantTerwijlEersteTijdstipBezetIs_PlantOpEerstvolgendeVrijeSlot()
        {
            await AfsAppointmentsQueries.CreateAppointmentAsync(Jan, DeVries, FirstSlot);

            var plan = await OpenPlanningAsync(Driver, "medewerker.a");
            Book(plan, Piet);

            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot));
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot.AddHours(1)));
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsOfPatientAsync(Piet));
        }

        [TestMethod, TestCategory("TC-AFS-03_6_2")]
        public async Task TC_AFS_03_6_2_Medewerker_PlantTussenTweeBestaandeAfspraken()
        {
            var middleSlot = FirstSlot.AddHours(1);
            var afterSlot = FirstSlot.AddHours(2);

            await AfsAppointmentsQueries.CreateAppointmentAsync(Jan, DeVries, FirstSlot);
            await AfsAppointmentsQueries.CreateAppointmentAsync(Piet, DeVries, afterSlot);

            var plan = await OpenPlanningAsync(Driver, "medewerker.a");
            Book(plan, Anna);

            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot));
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, middleSlot));
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, afterSlot));
        }

        // =========================================================
        // AC-AFS-03_7 – Twee medewerkers boeken tegelijk
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_7_1")]
        public async Task TC_AFS_03_7_1_TweeMedewerkers_BoekenParalleel_GeenDubbeleBoekingOpZelfdeTijdstip()
        {
            var patientJan = await AfsAppointmentsQueries.PatientIdAsync(Jan);
            var patientPiet = await AfsAppointmentsQueries.PatientIdAsync(Piet);
            var specialistId = await AfsAppointmentsQueries.SpecialistIdAsync(DeVries);

            using var driverB = ExtraBrowser.Start();

            Driver.Navigate().GoToUrl(TestSettings.ClientUrl);
            driverB.Navigate().GoToUrl(TestSettings.ClientUrl);

            for (var run = 1; run <= 10; run++)
            {
                await AfsAppointmentsQueries.ResetAppointmentsAsync();
                var referralsBefore = await AfsAppointmentsQueries.CountReferralsAsync();

                var taskA = Task.Run(() =>
                    BrowserApi.PostAppointmentWithResponse(Driver, patientJan, specialistId));

                var taskB = Task.Run(() =>
                    BrowserApi.PostAppointmentWithResponse(driverB, patientPiet, specialistId));

                var responses = await Task.WhenAll(taskA, taskB);

                var succeededCount = responses.Count(r => r.Status is >= 200 and < 300);
                var conflictCount = responses.Count(r => r.Status is 400 or 409 or 422);

                Assert.AreEqual(2, succeededCount + conflictCount,
                    $"Run {run}: A={responses[0].Status}, B={responses[1].Status}");

                Assert.IsTrue(await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot) <= 1,
                    $"Run {run}: geen dubbele boeking op exact hetzelfde tijdstip.");

                Assert.AreEqual(succeededCount,
                    await AfsAppointmentsQueries.CountTotalAppointmentsOfSpecialistAsync(DeVries));

                Assert.AreEqual(referralsBefore + succeededCount,
                    await AfsAppointmentsQueries.CountReferralsAsync());
            }
        }

        // =========================================================
        // AC-AFS-03_8 – Afspraak buiten beschikbaarheid (persoonlijke iCal-agenda)
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_8_1")]
        public async Task TC_AFS_03_8_1_Medewerker_PlantWanneerAlleSpecialistenVolZijn_WordtGeweigerd()
        {
            await AfsAppointmentsQueries.BlockSpecialistExternalAsync(DeVries, WindowStart, WindowEnd);

            var referralsBefore = await AfsAppointmentsQueries.CountReferralsAsync();
            var plan = await OpenPlanningAsync(Driver, "medewerker.a");

            Book(plan, Jan);

            Assert.IsTrue(plan.HasErrorMessage, plan.Message);
            StringAssert.Contains(plan.Message, MsgNoSpecialist);
            Assert.AreEqual(0, await AfsAppointmentsQueries.CountTotalAppointmentsOfSpecialistAsync(DeVries));
            Assert.AreEqual(referralsBefore, await AfsAppointmentsQueries.CountReferralsAsync());
        }

        [TestMethod, TestCategory("TC-AFS-03_8_2")]
        public async Task TC_AFS_03_8_2_Medewerker_PlantOpVolgendeWerkdag_WanneerEersteWerkdagInPersoonlijkeAgendaBezetIs()
        {
            await AfsAppointmentsQueries.BlockSpecialistExternalAsync(
                DeVries, FirstSlot, FirstSlot.Date.AddHours(17));

            var expectedSlot = NextWorkdaySlot(FirstSlot);
            var plan = await OpenPlanningAsync(Driver, "medewerker.a");

            Book(plan, Jan);

            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);
            Assert.AreEqual(0, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot));
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, expectedSlot));
        }

        [TestMethod, TestCategory("TC-AFS-03_8_3")]
        public async Task TC_AFS_03_8_3_Backend_WeigertAfspraakWanneerGeenEnkeleSpecialistBinnen3WekenVrijIs()
        {
            await AfsAppointmentsQueries.BlockSpecialistExternalAsync(
                DeVries, WindowStart, WindowEnd);

            var referralsBefore = await AfsAppointmentsQueries.CountReferralsAsync();
            Driver.Navigate().GoToUrl(TestSettings.ClientUrl);

            var response = await PostAsync(Driver, Jan, DeVries);

            Assert.IsTrue(
                response.Status is 400 or 409 or 422,
                $"Verwacht weigering, kreeg {response.Status}.\nBackend response: {response.Body}");

            Assert.AreEqual(
                0,
                await AfsAppointmentsQueries.CountTotalAppointmentsOfSpecialistAsync(DeVries));

            Assert.AreEqual(
                referralsBefore,
                await AfsAppointmentsQueries.CountReferralsAsync());
        }
    }
}
