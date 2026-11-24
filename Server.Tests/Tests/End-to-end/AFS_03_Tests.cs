using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using Server.Models.Enums;
using Server.Tests.Helpers;
using Server.Tests.PageModels;
using Server.Tests.PageModels.AppointmentPages;
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
            "Geen beschikbare specialist gevonden.";

        /// <summary>
        /// De server plant op dit moment altijd op morgen 10:00 (hardcoded in
        /// PlanningService.SetSpecialistForAppointment) en kiest zelf een specialist:
        /// eerst de voorkeursspecialist, daarna de eerste beschikbare van het specialisme.
        /// Vervang deze regel zodra het tijdstip niet meer vast ligt.
        /// </summary>
        private static DateTime Slot =>
            DateTime.Today.AddDays(1).AddHours(10);

        // =========================================================
        // Helpers
        // =========================================================

        /// <summary>
        /// Alleen Dr. De Vries is beschikbaar (KNO, morgen). Daardoor is het resultaat van de
        /// automatische keuze van de server voorspelbaar: slaagt hij niet, dan is er niemand anders.
        /// Draait na de initialisatie van EndToEndTestBase.
        /// </summary>
        [TestInitialize]
        public async Task ArrangeAvailabilityAsync() =>
            await AfsData.MakeOnlyAvailableAsync(DeVries, Specialism.KNO, Slot);

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

        /// <summary>Vult het formulier in, bevestigt en wacht op de melding.</summary>
        private static void Book(
            PlanAppointmentPage plan,
            string patient,
            string? preferredSpecialist = DeVries)
        {
            plan.FillForm(
                patient,
                (int)Specialism.KNO,
                preferredSpecialist);

            plan.Submit();
            plan.WaitForMessage();
        }

        /// <summary>
        /// Plant rechtstreeks via de API (zonder formulier). De server negeert het tijdstip dat
        /// hier wordt meegestuurd en gebruikt zelf morgen 10:00.
        /// </summary>
        private static async Task<long> PostAsync(
            IWebDriver driver,
            string patient,
            string specialist)
        {
            var patientId = await AfsData.PatientIdAsync(patient);
            var specialistId = await AfsData.SpecialistIdAsync(specialist);

            return BrowserApi.PostAppointment(
                driver,
                patientId,
                specialistId,
                Slot
            );
        }

        // =========================================================
        // AC-AFS-03_1 – Medewerker plant een afspraak
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_1_1")]
        public async Task TC_AFS_03_1_1_Medewerker_PlantAfspraak_AfspraakEnVerwijzingZijnOpgeslagen()
        {
            // Arrange
            var referralsBefore =
                await AfsData.CountReferralsOfPatientAsync(Jan);

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a");

            // Act
            Book(plan, Jan);

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);

            Assert.AreEqual(
                "patient",
                plan.CurrentStepId,
                "Na het opslaan begint het formulier weer bij de eerste stap.");

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(DeVries, Slot));

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsOfPatientAsync(Jan));

            Assert.AreEqual(
                referralsBefore + 1,
                await AfsData.CountReferralsOfPatientAsync(Jan));

            Assert.AreEqual(
                "Ingepland",
                (await AfsData.AppointmentReferralAsync(
                    DeVries, Slot)).Status);
        }

        [TestMethod, TestCategory("TC-AFS-03_1_2")]
        public async Task TC_AFS_03_1_2_Medewerker_PlantTijdstipDirectNaBestaandeAfspraak_WordtOpgeslagen()
        {
            // Arrange: een afspraak van 09:00 duurt 60 minuten en eindigt dus precies om 10:00
            var before = Slot.AddHours(-1);

            await AfsData.CreateAppointmentAsync(
                Piet, DeVries, before);

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a");

            // Act
            Book(plan, Jan);

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(DeVries, before));

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(DeVries, Slot));
        }

        [TestMethod, TestCategory("TC-AFS-03_1_3")]
        [Ignore("Wacht op login: rol (huisarts/patiënt) kan nog niet worden aangenomen.")]
        public async Task TC_AFS_03_1_3_Huisarts_KanGeenAfspraakPlannen_BackendWeigert()
        {
            // Arrange
            var referralsBefore = await AfsData.CountReferralsAsync();

            LoginAs(Driver, "huisarts.visser");

            // Act
            var status = await PostAsync(
                Driver, Jan, DeVries);

            // Assert
            Assert.AreEqual(403, status);

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsAsync(DeVries, Slot));

            Assert.AreEqual(
                referralsBefore,
                await AfsData.CountReferralsAsync());
        }

        // =========================================================
        // AC-AFS-03_2 – Automatisch aanmaken van een verwijzing
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_2_1")]
        public async Task TC_AFS_03_2_1_Medewerker_PlantZonderBestaandeVerwijzing_VerwijzingWordtAangemaakt()
        {
            // Arrange
            Assert.AreEqual(
                0,
                await AfsData.CountReferralsOfPatientAsync(Piet));

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a");

            // Act
            Book(plan, Piet);

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsOfPatientAsync(Piet));

            Assert.AreEqual(
                1,
                await AfsData.CountReferralsOfPatientAsync(Piet));

            var referral =
                await AfsData.AppointmentReferralAsync(
                    DeVries, Slot);

            Assert.AreEqual("Ingepland", referral.Status);
            Assert.AreEqual((int)Specialism.KNO, referral.Specialism);
            Assert.AreEqual("KNO002", referral.TreatmentCode);
        }

        [TestMethod, TestCategory("TC-AFS-03_2_2")]
        public async Task TC_AFS_03_2_2_Medewerker_PlantVervolgafspraak_KrijgtEigenVerwijzing()
        {
            // Arrange: Jan heeft een eerdere afspraak, de nieuwe is de vervolgafspraak
            var earlier = Slot.AddDays(-7);

            await AfsData.CreateAppointmentAsync(
                Jan, DeVries, earlier);

            var referralsBefore =
                await AfsData.CountReferralsOfPatientAsync(Jan);

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a");

            // Act
            Book(plan, Jan);

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);

            Assert.AreEqual(
                2,
                await AfsData.CountAppointmentsOfPatientAsync(Jan));

            Assert.AreEqual(
                referralsBefore + 1,
                await AfsData.CountReferralsOfPatientAsync(Jan));

            Assert.AreEqual(
                "Ingepland",
                (await AfsData.AppointmentReferralAsync(
                    DeVries, earlier)).Status);

            Assert.AreEqual(
                "Ingepland",
                (await AfsData.AppointmentReferralAsync(
                    DeVries, Slot)).Status);
        }

        [TestMethod, TestCategory("TC-AFS-03_2_3")]
        public async Task TC_AFS_03_2_3_Backend_AccepteertAfspraakZonderVerwijzingEnMaaktErEenAan()
        {
            // Arrange
            Driver.Navigate().GoToUrl(TestSettings.ClientUrl);
            //LoginAs(Driver, "medewerker.a");

            // Act
            var status = await PostAsync(
                Driver, Piet, DeVries);

            // Assert
            Assert.IsTrue(
                status is >= 200 and < 300,
                $"Verwacht succesvolle boeking (2xx), kreeg {status}.");

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsOfPatientAsync(Piet));

            Assert.AreEqual(
                1,
                await AfsData.CountReferralsOfPatientAsync(Piet));
        }

        // =========================================================
        // AC-AFS-03_3 – Tijdstip is ondertussen door iemand anders geboekt
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_3_1")]
        public async Task TC_AFS_03_3_1_Medewerker_BoektTijdstipDatOndertussenIsGeboekt_KrijgtFoutmelding()
        {
            // Arrange
            using var driverB = ExtraBrowser.Start();

            var planA =
                await OpenPlanningAsync(Driver, "medewerker.a");

            var planB =
                await OpenPlanningAsync(driverB, "medewerker.b");

            // Beide medewerkers vullen het formulier in en staan op de controle-stap
            planA.FillForm(Jan, (int)Specialism.KNO, DeVries);
            planB.FillForm(Piet, (int)Specialism.KNO, DeVries);

            // Medewerker B bevestigt als eerste
            planB.Submit();
            planB.WaitForMessage();

            Assert.IsTrue(planB.HasSuccessMessage, planB.Message);

            var referralsBefore =
                await AfsData.CountReferralsAsync();

            // Act: medewerker A probeert hetzelfde tijdstip te boeken
            planA.Submit();
            planA.WaitForMessage();

            // Assert
            Assert.IsTrue(planA.HasErrorMessage, planA.Message);
            StringAssert.Contains(planA.Message, MsgNoSpecialist);

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Slot));

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsOfPatientAsync(Jan));

            Assert.AreEqual(
                referralsBefore,
                await AfsData.CountReferralsAsync(),
                "Er is een wees-verwijzing achtergebleven.");
        }

        /// <summary>
        /// Het tijdstip kan niet meer in het formulier gekozen worden. De test controleert daarom
        /// dat een medewerker na een mislukte boeking op dezelfde pagina opnieuw kan bevestigen
        /// zodra het tijdstip weer vrij is.
        /// </summary>
        [TestMethod, TestCategory("TC-AFS-03_3_2")]
        public async Task TC_AFS_03_3_2_Medewerker_BevestigtOpnieuwNaMislukteBoeking_WanneerTijdstipVrijIs()
        {
            // Arrange
            await AfsData.CreateAppointmentAsync(
                Piet, DeVries, Slot);

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a");

            plan.FillForm(Jan, (int)Specialism.KNO, DeVries);

            // Act 1: eerste poging mislukt
            plan.Submit();
            plan.WaitForMessage();

            Assert.IsTrue(plan.HasErrorMessage, plan.Message);

            Assert.AreEqual(
                "review",
                plan.CurrentStepId,
                "Na een fout blijft de medewerker op de controle-stap met de ingevulde gegevens.");

            // Act 2: het tijdstip komt vrij en de medewerker bevestigt opnieuw
            await AfsData.ResetAppointmentsAsync();

            plan.Submit();
            plan.WaitForSuccessMessage();

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Slot));

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsOfPatientAsync(Jan));

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsOfPatientAsync(Piet));
        }

        // =========================================================
        // AC-AFS-03_4 – Het handmatige afsprakenformulier
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_4_1")]
        public async Task TC_AFS_03_4_1_Afsprakenpagina_ToontAlleBenodigdeFormuliervelden()
        {
            // Arrange
            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a");

            // Assert: alleen de velden van de huidige stap staan in de pagina
            Assert.AreEqual("patient", plan.CurrentStepId);
            Assert.IsTrue(plan.HasField("patient-id"));
            Assert.IsTrue(plan.HasField("specialism"));

            plan.FillPatientStep(Jan, (int)Specialism.KNO).NextStep("care");
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
        // AC-AFS-03_5 – Patiënt kan zelf geen afspraak plannen
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_5_1")]
        [Ignore("Wacht op login: rol (huisarts/patiënt) kan nog niet worden aangenomen.")]
        public void TC_AFS_03_5_1_Patient_HeeftGeenFunctieOmZelfAfspraakTePlannen()
        {
            // Arrange
            LoginAs(Driver, "jan.jansen");

            var nav = new PortalNavigation(Driver);
            var appointments = new PatientAppointmentsPage(Driver);

            // Act
            nav.GoToReferrals();
            var onReferrals = appointments.HasPlanFunction;

            nav.GoToAppointments();
            var onAppointments = appointments.HasPlanFunction;

            nav.GoToHome();
            var onHome = appointments.HasPlanFunction;

            // Assert
            Assert.IsFalse(onReferrals);
            Assert.IsFalse(onAppointments);
            Assert.IsFalse(onHome);
        }

        [TestMethod, TestCategory("TC-AFS-03_5_2")]
        [Ignore("Wacht op login: rol (huisarts/patiënt) kan nog niet worden aangenomen.")]
        public async Task TC_AFS_03_5_2_Backend_WeigertAfspraakVanPatient_Met403()
        {
            // Arrange
            var referralsBefore =
                await AfsData.CountReferralsAsync();

            LoginAs(Driver, "jan.jansen");

            // Act
            var status = await PostAsync(
                Driver, Jan, DeVries);

            // Assert
            Assert.AreEqual(403, status);

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsOfPatientAsync(Jan));

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Slot));

            Assert.AreEqual(
                referralsBefore,
                await AfsData.CountReferralsAsync());
        }

        // =========================================================
        // AC-AFS-03_6 – Specialist heeft al een afspraak
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_6_1")]
        public async Task TC_AFS_03_6_1_Medewerker_PlantOpBezetTijdstip_WordtGeweigerd()
        {
            // Arrange
            await AfsData.CreateAppointmentAsync(
                Jan, DeVries, Slot);

            var referralsBefore =
                await AfsData.CountReferralsAsync();

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a");

            // Act
            Book(plan, Piet);

            // Assert
            Assert.IsTrue(plan.HasErrorMessage, plan.Message);
            StringAssert.Contains(plan.Message, MsgNoSpecialist);

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Slot));

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsOfPatientAsync(Piet));

            Assert.AreEqual(
                referralsBefore,
                await AfsData.CountReferralsAsync(),
                "Er is een wees-verwijzing achtergebleven.");
        }

        [TestMethod, TestCategory("TC-AFS-03_6_2")]
        public async Task TC_AFS_03_6_2_Medewerker_PlantTussenTweeBestaandeAfspraken()
        {
            // Arrange: afspraken die precies voor (09:00-10:00) en na (11:00-12:00) het tijdstip liggen
            var before = Slot.AddHours(-1);
            var after = Slot.AddHours(1);

            await AfsData.CreateAppointmentAsync(
                Jan, DeVries, before);

            await AfsData.CreateAppointmentAsync(
                Piet, DeVries, after);

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a");

            // Act
            Book(plan, Anna);

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, before));

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Slot));

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, after));
        }

        // =========================================================
        // AC-AFS-03_7 – Twee medewerkers boeken tegelijk
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_7_1")]
        public async Task TC_AFS_03_7_1_TweeMedewerkers_BoekenParalleelHetzelfdeTijdstip()
        {
            // Arrange
            var patientJan =
                await AfsData.PatientIdAsync(Jan);

            var patientPiet =
                await AfsData.PatientIdAsync(Piet);

            var specialistId =
                await AfsData.SpecialistIdAsync(DeVries);

            using var driverB = ExtraBrowser.Start();

            // Beide browsers moeten de toegestane origin hebben.
            Driver.Navigate().GoToUrl(TestSettings.ClientUrl);
            driverB.Navigate().GoToUrl(TestSettings.ClientUrl);

            //LoginAs(Driver, "medewerker.a");
            //LoginAs(driverB, "medewerker.b");

            // Dit blijft een API-test: twee klikken in de UI zijn niet gelijktijdig te krijgen.
            for (var run = 1; run <= 10; run++)
            {
                await AfsData.ResetAppointmentsAsync();

                var referralsBefore =
                    await AfsData.CountReferralsAsync();

                // Act: beide verzoeken tegelijkertijd versturen.
                var taskA = Task.Run(() =>
                    BrowserApi.PostAppointment(
                        Driver,
                        patientJan,
                        specialistId,
                        Slot));

                var taskB = Task.Run(() =>
                    BrowserApi.PostAppointment(
                        driverB,
                        patientPiet,
                        specialistId,
                        Slot));

                var statuses = await Task.WhenAll(taskA, taskB);

                // Assert
                Assert.AreEqual(
                    1,
                    await AfsData.CountAppointmentsAsync(
                        DeVries, Slot),
                    $"Run {run}: verwacht precies één afspraak.");

                Assert.AreEqual(
                    1,
                    statuses.Count(s => s is >= 200 and < 300),
                    $"Run {run}: precies één verzoek moet slagen.");

                Assert.AreEqual(
                    1,
                    statuses.Count(s => s is 400 or 409 or 422),
                    $"Run {run}: het andere verzoek moet geweigerd worden.");

                Assert.AreEqual(
                    referralsBefore + 1,
                    await AfsData.CountReferralsAsync(),
                    $"Run {run}: de geweigerde boeking mag geen verwijzing achterlaten.");
            }
        }

        // =========================================================
        // AC-AFS-03_8 – Afspraak buiten beschikbaarheid
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_8_1")]
        public async Task TC_AFS_03_8_1_Medewerker_PlantOpNietBeschikbareDag_WordtGeweigerd()
        {
            // Arrange
            await AfsData.SetUnavailableAsync(
                DeVries, Slot);

            var referralsBefore =
                await AfsData.CountReferralsAsync();

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a");

            // Act
            Book(plan, Jan);

            // Assert
            Assert.IsTrue(plan.HasErrorMessage, plan.Message);
            StringAssert.Contains(plan.Message, MsgNoSpecialist);

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Slot));

            Assert.AreEqual(
                referralsBefore,
                await AfsData.CountReferralsAsync());
        }

        [TestMethod, TestCategory("TC-AFS-03_8_2")]
        public async Task TC_AFS_03_8_2_Medewerker_PlantDagMetNietBeschikbareDagenErvoorEnErna()
        {
            // Arrange: de dag ervoor en de dag erna is de specialist niet beschikbaar,
            // maar op de dag zelf wel (zie ArrangeAvailabilityAsync).
            await AfsData.SetUnavailableAsync(
                DeVries, Slot.AddDays(-1));

            await AfsData.SetUnavailableAsync(
                DeVries, Slot.AddDays(1));

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a");

            // Act
            Book(plan, Jan);

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Slot));
        }

        [TestMethod, TestCategory("TC-AFS-03_8_3")]
        public async Task TC_AFS_03_8_3_Backend_WeigertAfspraakBuitenBeschikbaarheid()
        {
            // Arrange
            await AfsData.SetUnavailableAsync(
                DeVries, Slot);

            var referralsBefore =
                await AfsData.CountReferralsAsync();

            Driver.Navigate().GoToUrl(TestSettings.ClientUrl);

            //LoginAs(Driver, "medewerker.a");

            // Act
            var status = await PostAsync(
                Driver, Jan, DeVries);

            // Assert
            Assert.IsTrue(
                status is 400 or 409 or 422,
                $"Verwacht weigering, kreeg {status}.");

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Slot));

            Assert.AreEqual(
                referralsBefore,
                await AfsData.CountReferralsAsync());
        }
    }
}