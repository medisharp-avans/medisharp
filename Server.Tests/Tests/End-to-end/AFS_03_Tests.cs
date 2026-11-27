
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

        private const string MsgSlotTaken =
            "Dit tijdslot is niet meer beschikbaar";

        private static readonly DateTime Jan12_0900 =
            new(2027, 1, 12, 9, 0, 0);

        private static readonly DateTime Jan12_1000 =
            new(2027, 1, 12, 10, 0, 0);

        private static readonly DateTime Jan12_1100 =
            new(2027, 1, 12, 11, 0, 0);

        private static readonly DateTime Jan19_1100 =
            new(2027, 1, 19, 11, 0, 0);

        // =========================================================
        // Helpers
        // =========================================================

        private static void LoginAs(IWebDriver driver, string email)
        {
            new LoginPage(driver).LoginAs(email);
        }

        private Task<PlanAppointmentPage> OpenPlanningAsync(
            IWebDriver driver,
            string staffEmail)
        {
            LoginAs(driver, staffEmail);

            var plan = new PlanAppointmentPage(driver).Open();

            return Task.FromResult(plan);
        }

        private static async Task Book(
            PlanAppointmentPage plan,
            string patient,
            string specialist,
            DateTime start)
        {
            var patientId = await AfsData.PatientIdAsync(patient);
            var specialistId = await AfsData.SpecialistIdAsync(specialist);

            plan.FillForm(
                patientId,
                specialistId,
                start,
                generalPractitionerId: 1,
                specialism: (int)Specialism.KNO
            );

            plan.Submit();
            plan.WaitForMessage();
        }

        private static async Task<long> PostAsync(
            IWebDriver driver,
            string patient,
            string specialist,
            DateTime start)
        {
            var patientId = await AfsData.PatientIdAsync(patient);
            var specialistId = await AfsData.SpecialistIdAsync(specialist);

            return BrowserApi.PostAppointment(
                driver,
                patientId,
                specialistId,
                start
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
                await OpenPlanningAsync(Driver, "medewerker.a@test.nl");

            // Act
            await Book(plan, Jan, DeVries, Jan12_1000);

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsOfPatientAsync(Jan));

            Assert.AreEqual(
                referralsBefore + 1,
                await AfsData.CountReferralsOfPatientAsync(Jan));

            Assert.AreEqual(
                "Ingepland",
                (await AfsData.AppointmentReferralAsync(
                    DeVries, Jan12_1000)).Status);
        }

        [TestMethod, TestCategory("TC-AFS-03_1_2")]
        public async Task TC_AFS_03_1_2_Medewerker_PlantTijdstipDirectNaBestaandeAfspraak_WordtOpgeslagen()
        {
            // Arrange: afspraak van 09:00 duurt 60 minuten
            await AfsData.CreateAppointmentAsync(
                Piet, DeVries, Jan12_0900);

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a@test.nl");

            // Act
            await Book(plan, Jan, DeVries, Jan12_1000);

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(DeVries, Jan12_0900));

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
        }

        [TestMethod, TestCategory("TC-AFS-03_1_3")]
        [Ignore("Wacht op login: rol (huisarts/patiënt) kan nog niet worden aangenomen.")]
        public async Task TC_AFS_03_1_3_Huisarts_KanGeenAfspraakPlannen_BackendWeigert()
        {
            // Arrange
            var referralsBefore = await AfsData.CountReferralsAsync();

            LoginAs(Driver, "visser@test.nl");

            // Act
            var status = await PostAsync(
                Driver, Jan, DeVries, Jan12_1000);

            // Assert
            Assert.AreEqual(403, status);

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));

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
                await OpenPlanningAsync(Driver, "medewerker.a@test.nl");

            // Act
            await Book(plan, Piet, DeVries, Jan12_1000);

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
                    DeVries, Jan12_1000);

            Assert.AreEqual("Ingepland", referral.Status);
            Assert.AreEqual((int)Specialism.KNO, referral.Specialism);
            Assert.AreEqual("KNO002", referral.TreatmentCode);
        }

        [TestMethod, TestCategory("TC-AFS-03_2_2")]
        public async Task TC_AFS_03_2_2_Medewerker_PlantVervolgafspraak_KrijgtEigenVerwijzing()
        {
            // Arrange
            await AfsData.CreateAppointmentAsync(
                Jan, DeVries, Jan12_1000);

            var referralsBefore =
                await AfsData.CountReferralsOfPatientAsync(Jan);

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a@test.nl");

            // Act
            await Book(plan, Jan, DeVries, Jan19_1100);

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
                    DeVries, Jan12_1000)).Status);

            Assert.AreEqual(
                "Ingepland",
                (await AfsData.AppointmentReferralAsync(
                    DeVries, Jan19_1100)).Status);
        }

        [TestMethod, TestCategory("TC-AFS-03_2_3")]
        public async Task TC_AFS_03_2_3_Backend_AccepteertAfspraakZonderVerwijzingEnMaaktErEenAan()
        {
            // Arrange
            Driver.Navigate().GoToUrl(TestSettings.ClientUrl);
            //LoginAs(Driver, "medewerker.a@test.nl");

            // Act
            var status = await PostAsync(
                Driver, Piet, DeVries, Jan12_1000);

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
                await OpenPlanningAsync(Driver, "medewerker.a@test.nl");

            var planB =
                await OpenPlanningAsync(driverB, "medewerker.b@test.nl");

            var patientJan = await AfsData.PatientIdAsync(Jan);
            var patientPiet = await AfsData.PatientIdAsync(Piet);
            var specialistId = await AfsData.SpecialistIdAsync(DeVries);

            planA.FillForm(
                patientJan,
                specialistId,
                Jan12_1000,
                1,
                (int)Specialism.KNO);

            planB.FillForm(
                patientPiet,
                specialistId,
                Jan12_1000,
                1,
                (int)Specialism.KNO);

            // Medewerker B boekt als eerste
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

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Jan12_1000));

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsOfPatientAsync(Jan));

            Assert.AreEqual(
                referralsBefore,
                await AfsData.CountReferralsAsync(),
                "Er is een wees-verwijzing achtergebleven.");
        }

        [TestMethod, TestCategory("TC-AFS-03_3_2")]
        public async Task TC_AFS_03_3_2_Medewerker_KiestAnderTijdstipNaMislukteBoeking()
        {
            // Arrange
            await AfsData.CreateAppointmentAsync(
                Piet, DeVries, Jan12_1000);

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a@test.nl");

            // Act
            await Book(plan, Jan, DeVries, Jan12_1100);

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Jan12_1000));

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Jan12_1100));

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsOfPatientAsync(Jan));
        }

        // =========================================================
        // AC-AFS-03_4 – Het handmatige afsprakenformulier
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-03_4_1")]
        public async Task TC_AFS_03_4_1_Afsprakenpagina_ToontAlleBenodigdeFormuliervelden()
        {
            // Arrange
            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a@test.nl");

            // Assert
            Assert.IsTrue(plan.HasField("patient-id"));
            Assert.IsTrue(plan.HasField("specialist-id"));
            Assert.IsTrue(plan.HasField("start-time"));
            Assert.IsTrue(plan.HasField("general-practitioner-id"));
            Assert.IsTrue(plan.HasField("specialism"));
            Assert.IsTrue(plan.HasField("reason"));
            Assert.IsTrue(plan.HasField("urgency"));
            Assert.IsTrue(plan.HasField("treatment-code"));
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
            LoginAs(Driver, "jan.jansen@test.nl");

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

            LoginAs(Driver, "jan.jansen@test.nl");

            // Act
            var status = await PostAsync(
                Driver, Jan, DeVries, Jan12_1000);

            // Assert
            Assert.AreEqual(403, status);

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsOfPatientAsync(Jan));

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Jan12_1000));

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
                Jan, DeVries, Jan12_1000);

            var referralsBefore =
                await AfsData.CountReferralsAsync();

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a@test.nl");

            // Act
            await Book(plan, Piet, DeVries, Jan12_1000);

            // Assert
            Assert.IsTrue(plan.HasErrorMessage, plan.Message);

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Jan12_1000));

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsOfPatientAsync(Piet));

            Assert.AreEqual(
                referralsBefore,
                await AfsData.CountReferralsAsync(),
                "Er is een wees-verwijzing achtergebleven.");
        }

        [TestMethod, TestCategory("TC-AFS-03_6_2")]
        public async Task TC_AFS_03_6_2_Medewerker_PlantDirectVoorEnNaBestaandeAfspraak()
        {
            // Arrange
            await AfsData.CreateAppointmentAsync(
                Jan, DeVries, Jan12_1000);

            // Act
            var planPiet =
                await OpenPlanningAsync(Driver, "medewerker.a@test.nl");

            await Book(planPiet, Piet, DeVries, Jan12_0900);

            var planAnna =
                await OpenPlanningAsync(Driver, "medewerker.a@test.nl");

            await Book(planAnna, Anna, DeVries, Jan12_1100);

            // Assert
            Assert.IsTrue(planPiet.HasSuccessMessage, planPiet.Message);
            Assert.IsTrue(planAnna.HasSuccessMessage, planAnna.Message);

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Jan12_0900));

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Jan12_1000));

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Jan12_1100));
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

            //LoginAs(Driver, "medewerker.a@test.nl");
            //LoginAs(driverB, "medewerker.b@test.nl");

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
                        Jan12_1000));

                var taskB = Task.Run(() =>
                    BrowserApi.PostAppointment(
                        driverB,
                        patientPiet,
                        specialistId,
                        Jan12_1000));

                var statuses = await Task.WhenAll(taskA, taskB);

                // Assert
                Assert.AreEqual(
                    1,
                    await AfsData.CountAppointmentsAsync(
                        DeVries, Jan12_1000),
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
                DeVries, Jan12_1000);

            var referralsBefore =
                await AfsData.CountReferralsAsync();

            var plan =
                await OpenPlanningAsync(Driver, "medewerker.a@test.nl");

            // Act
            await Book(plan, Jan, DeVries, Jan12_1000);

            // Assert
            Assert.IsTrue(plan.HasErrorMessage, plan.Message);

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Jan12_1000));

            Assert.AreEqual(
                referralsBefore,
                await AfsData.CountReferralsAsync());
        }

        [TestMethod, TestCategory("TC-AFS-03_8_2")]
        public async Task TC_AFS_03_8_2_Medewerker_PlantDagVoorEnDagNaNietBeschikbareDag()
        {
            // Arrange
            var jan11 = new DateTime(2027, 1, 11, 10, 0, 0);
            var jan13 = new DateTime(2027, 1, 13, 10, 0, 0);

            await AfsData.SetUnavailableAsync(
                DeVries, Jan12_1000);

            // Act
            var planJan =
                await OpenPlanningAsync(Driver, "medewerker.a@test.nl");

            await Book(planJan, Jan, DeVries, jan11);

            var planPiet =
                await OpenPlanningAsync(Driver, "medewerker.a@test.nl");

            await Book(planPiet, Piet, DeVries, jan13);

            // Assert
            Assert.IsTrue(planJan.HasSuccessMessage, planJan.Message);
            Assert.IsTrue(planPiet.HasSuccessMessage, planPiet.Message);

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, jan11));

            Assert.AreEqual(
                1,
                await AfsData.CountAppointmentsAsync(
                    DeVries, jan13));
        }

        [TestMethod, TestCategory("TC-AFS-03_8_3")]
        public async Task TC_AFS_03_8_3_Backend_WeigertAfspraakBuitenBeschikbaarheid()
        {
            // Arrange
            await AfsData.SetUnavailableAsync(
                DeVries, Jan12_1000);

            var referralsBefore =
                await AfsData.CountReferralsAsync();

            Driver.Navigate().GoToUrl(TestSettings.ClientUrl);

            //LoginAs(Driver, "medewerker.a@test.nl");

            // Act
            var status = await PostAsync(
                Driver, Jan, DeVries, Jan12_1000);

            // Assert
            Assert.IsTrue(
                status is 400 or 409 or 422,
                $"Verwacht weigering, kreeg {status}.");

            Assert.AreEqual(
                0,
                await AfsData.CountAppointmentsAsync(
                    DeVries, Jan12_1000));

            Assert.AreEqual(
                referralsBefore,
                await AfsData.CountReferralsAsync());
        }
    }
}
