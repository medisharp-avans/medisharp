using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using Server.Models.Enums;
using Server.Tests.Helpers;
using Server.Tests.PageModels;
using Server.Tests.PageModels.AppointmentPages;

namespace Server.Tests.EndToEnd
{
    /// <summary>
    /// De medewerker plant alleen via de afsprakenpagina. Er hoeft geen verwijzing te bestaan:
    /// bij elke afspraak maakt de applicatie zelf een verwijzing aan (status "Ingepland").
    /// </summary>
    [TestClass]
    public class AFS_03_Tests : EndToEndTestBase
    {
        private const string DeVries = "Dr. De Vries";
        private const string Bakker = "Dr. Bakker";
        private const string Smit = "Dr. Smit";
        private const string Jan = "Jan Jansen";
        private const string Piet = "Piet Pietersen";
        private const string Anna = "Anna de Boer";

        private const string MsgSlotTaken = "Dit tijdslot is niet meer beschikbaar";
        private const string MsgSpecialistBusy = "Specialist is al ingepland";

        private static readonly DateTime Jan12_0900 = new(2027, 1, 12, 9, 0, 0);
        private static readonly DateTime Jan12_1000 = new(2027, 1, 12, 10, 0, 0);
        private static readonly DateTime Jan12_1100 = new(2027, 1, 12, 11, 0, 0);
        private static readonly DateTime Jan19_1100 = new(2027, 1, 19, 11, 0, 0);

        private static void LoginAs(IWebDriver driver, string username)
        {
            // new LoginPage(driver).LoginAs(username);
        }

        private async Task<PlanAppointmentPage> OpenPlanningAsync(IWebDriver driver, string staffUser, string patient)
        {
            LoginAs(driver, staffUser);
            var patientId = await AfsData.PatientIdAsync(patient);
            return new PlanAppointmentPage(driver).Open(patientId).OpenTimeSlots();
        }

        private static void Book(PlanAppointmentPage plan, string specialist, DateTime start)
        {
            plan.SelectSlot(specialist, start);
            plan.Confirm();
        }

        /// <summary>POST naar /api/appointments. De namen worden omgezet naar ids, want de DTO verwacht PatientId en SpecialistId.</summary>
        private static async Task<long> PostAsync(IWebDriver driver, string patient, string specialist, DateTime start)
        {
            var patientId = await AfsData.PatientIdAsync(patient);
            var specialistId = await AfsData.SpecialistIdAsync(specialist);
            return BrowserApi.PostAppointment(driver, patientId, specialistId, start);
        }

        // =====================================================================
        // AC-AFS-03_1 – Medewerker plant een afspraak bij een beschikbare specialist
        // =====================================================================

        [TestMethod, TestCategory("TC-AFS-03_1_1")]
        public async Task TC_AFS_03_1_1_Medewerker_PlantAfspraak_AfspraakEnVerwijzingZijnOpgeslagen()
        {
            // Arrange
            var referralsBefore = await AfsData.CountReferralsOfPatientAsync(Jan);
            var plan = await OpenPlanningAsync(Driver, "medewerker.a", Jan);

            // Act
            Book(plan, DeVries, Jan12_1000);

            // Assert: afspraak, automatische verwijzing met status, tijdslot bezet
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual(1, await AfsData.CountAppointmentsOfPatientAsync(Jan));
            Assert.AreEqual(referralsBefore + 1, await AfsData.CountReferralsOfPatientAsync(Jan));
            Assert.AreEqual("Ingepland", (await AfsData.AppointmentReferralAsync(DeVries, Jan12_1000)).Status);

            var planPiet = await OpenPlanningAsync(Driver, "medewerker.a", Piet);
            Assert.IsFalse(planPiet.HasSlot(DeVries, Jan12_1000), "Het geboekte tijdslot wordt nog aangeboden.");
        }

        [TestMethod, TestCategory("TC-AFS-03_1_2")]
        public async Task TC_AFS_03_1_2_Medewerker_PlantTijdslotDirectNaBestaandeAfspraak_WordtOpgeslagenZonderConflict()
        {
            // Arrange: afspraak van 09:00 duurt 60 minuten (KNO002), dus 10:00 sluit direct aan
            await AfsData.CreateAppointmentAsync(Piet, DeVries, Jan12_0900);
            var plan = await OpenPlanningAsync(Driver, "medewerker.a", Jan);

            // Act
            Book(plan, DeVries, Jan12_1000);

            // Assert
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_0900), "Bestaande afspraak is gewijzigd.");
            Assert.IsFalse(plan.HasMessage && plan.Message.Contains(MsgSpecialistBusy));
        }

        [TestMethod, TestCategory("TC-AFS-03_1_3")]
        [Ignore("Wacht op login: rol (huisarts/patiënt) kan nog niet worden aangenomen.")]
        public async Task TC_AFS_03_1_3_Huisarts_KanGeenAfspraakPlannen_InterfaceEnBackendWeigeren()
        {
            // Arrange
            var referralsBefore = await AfsData.CountReferralsAsync();
            LoginAs(Driver, "huisarts.visser");

            // Act
            var hasUiFunction = new PatientAppointmentsPage(Driver).Open().HasPlanFunction;
            var status = await PostAsync(Driver, Jan, DeVries, Jan12_1000);

            // Assert
            Assert.IsFalse(hasUiFunction, "Huisarts ziet een functie om te plannen.");
            Assert.AreEqual(403, status);
            Assert.AreEqual(0, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual(referralsBefore, await AfsData.CountReferralsAsync());
        }

        // =====================================================================
        // AC-AFS-03_2 – Zonder bestaande verwijzing wordt er automatisch een verwijzing aangemaakt
        // =====================================================================

        [TestMethod, TestCategory("TC-AFS-03_2_1")]
        public async Task TC_AFS_03_2_1_Medewerker_PlantZonderBestaandeVerwijzing_VerwijzingWordtAutomatischAangemaakt()
        {
            // Arrange: Piet heeft geen enkele verwijzing
            Assert.AreEqual(0, await AfsData.CountReferralsOfPatientAsync(Piet), "Piet hoort geen verwijzing te hebben in de seed.");
            var plan = await OpenPlanningAsync(Driver, "medewerker.a", Piet);
            Assert.IsTrue(plan.HasSlot(DeVries, Jan12_1000), "Tijdslot wordt niet aangeboden voor een patiënt zonder verwijzing.");

            // Act
            Book(plan, DeVries, Jan12_1000);

            // Assert
            Assert.AreEqual(1, await AfsData.CountAppointmentsOfPatientAsync(Piet));
            Assert.AreEqual(1, await AfsData.CountReferralsOfPatientAsync(Piet));

            var referral = await AfsData.AppointmentReferralAsync(DeVries, Jan12_1000);
            Assert.AreEqual("Ingepland", referral.Status);
            Assert.AreEqual((int)Specialism.KNO, referral.Specialism);
            Assert.AreEqual("KNO002", referral.TreatmentCode);
        }

        [TestMethod, TestCategory("TC-AFS-03_2_2")]
        public async Task TC_AFS_03_2_2_Medewerker_PlantVervolgafspraak_KrijgtEigenVerwijzing()
        {
            // Arrange: Jan heeft al een afspraak (met verwijzing)
            await AfsData.CreateAppointmentAsync(Jan, DeVries, Jan12_1000);
            var referralsBefore = await AfsData.CountReferralsOfPatientAsync(Jan);
            var plan = await OpenPlanningAsync(Driver, "medewerker.a", Jan);
            Assert.IsTrue(plan.HasSlot(DeVries, Jan19_1100), "Tijdslot voor de vervolgafspraak wordt niet aangeboden.");

            // Act
            Book(plan, DeVries, Jan19_1100);

            // Assert: twee afspraken, elk met een eigen verwijzing, de bestaande blijft ongewijzigd
            Assert.AreEqual(2, await AfsData.CountAppointmentsOfPatientAsync(Jan));
            Assert.AreEqual(referralsBefore + 1, await AfsData.CountReferralsOfPatientAsync(Jan));
            Assert.AreEqual("Ingepland", (await AfsData.AppointmentReferralAsync(DeVries, Jan12_1000)).Status);
            Assert.AreEqual("Ingepland", (await AfsData.AppointmentReferralAsync(DeVries, Jan19_1100)).Status);
        }

        [TestMethod, TestCategory("TC-AFS-03_2_3")]
        public async Task TC_AFS_03_2_3_Backend_AccepteertAfspraakZonderVerwijzingEnMaaktErEenAan()
        {
            // Arrange
            LoginAs(Driver, "medewerker.a");

            // Act: het verzoek bevat geen verwijzing
            var status = await PostAsync(Driver, Piet, DeVries, Jan12_1000);

            // Assert
            Assert.IsTrue(status is >= 200 and < 300, $"Verwacht succesvolle boeking (2xx), kreeg {status}.");
            Assert.AreEqual(1, await AfsData.CountAppointmentsOfPatientAsync(Piet));
            Assert.AreEqual(1, await AfsData.CountReferralsOfPatientAsync(Piet));
        }

        // =====================================================================
        // AC-AFS-03_3 – Tijdslot is ondertussen door iemand anders gekozen
        // =====================================================================

        [TestMethod, TestCategory("TC-AFS-03_3_1")]
        public async Task TC_AFS_03_3_1_Medewerker_BevestigtTijdslotDatOndertussenIsGeboekt_KrijgtMeldingEnGeenWeesVerwijzing()
        {
            // Arrange
            using var driverB = ExtraBrowser.Start();
            var planA = await OpenPlanningAsync(Driver, "medewerker.a", Jan);
            var planB = await OpenPlanningAsync(driverB, "medewerker.b", Piet);
            planA.SelectSlot(DeVries, Jan12_1000);
            planB.SelectSlot(DeVries, Jan12_1000);
            planB.Confirm();
            var referralsBefore = await AfsData.CountReferralsAsync();

            // Act
            planA.Confirm();

            // Assert
            StringAssert.Contains(planA.WaitForMessage().Message, MsgSlotTaken);
            Assert.IsFalse(planA.HasSlot(DeVries, Jan12_1000), "Bezet tijdslot staat nog in de lijst.");
            Assert.IsTrue(planA.HasSlot(DeVries, Jan12_1100), "Overige vrije tijdsloten worden niet getoond.");
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual(0, await AfsData.CountAppointmentsOfPatientAsync(Jan));
            Assert.AreEqual(referralsBefore, await AfsData.CountReferralsAsync(), "Er is een wees-verwijzing achtergebleven.");
        }

        [TestMethod, TestCategory("TC-AFS-03_3_2")]
        public async Task TC_AFS_03_3_2_Medewerker_KiestAlternatiefTijdslotNaMislukteBoeking()
        {
            // Arrange
            await AfsData.CreateAppointmentAsync(Piet, DeVries, Jan12_1000);
            var plan = await OpenPlanningAsync(Driver, "medewerker.a", Jan);
            Assert.IsFalse(plan.HasSlot(DeVries, Jan12_1000));

            // Act
            Book(plan, DeVries, Jan12_1100);

            // Assert
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1100));
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual(1, await AfsData.CountAppointmentsOfPatientAsync(Jan));
        }

        // =====================================================================
        // AC-AFS-03_4 – Alleen tijdsloten van het gekozen specialisme
        // =====================================================================

        [TestMethod, TestCategory("TC-AFS-03_4_1")]
        public async Task TC_AFS_03_4_1_Tijdsloten_ToonUitsluitendSpecialistenVanHetGekozenSpecialisme()
        {
            // Arrange + Act: de pagina kiest KNO
            var plan = await OpenPlanningAsync(Driver, "medewerker.a", Jan);

            // Assert
            Assert.IsTrue(plan.HasAnySlotOf(DeVries), "Tijdsloten van Dr. De Vries ontbreken.");
            Assert.IsTrue(plan.HasAnySlotOf(Bakker), "Tijdsloten van Dr. Bakker ontbreken.");
            Assert.IsFalse(plan.HasAnySlotOf(Smit), "Tijdsloten van Dr. Smit (Cardiologie) mogen niet getoond worden.");
        }

        // =====================================================================
        // AC-AFS-03_5 – Patiënt kan zelf geen afspraak plannen
        // =====================================================================

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
            var referralsBefore = await AfsData.CountReferralsAsync();
            LoginAs(Driver, "jan.jansen");

            // Act
            var status = await PostAsync(Driver, Jan, DeVries, Jan12_1000);

            // Assert
            Assert.AreEqual(403, status);
            Assert.AreEqual(0, await AfsData.CountAppointmentsOfPatientAsync(Jan));
            Assert.AreEqual(0, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual(referralsBefore, await AfsData.CountReferralsAsync());
        }

        // =====================================================================
        // AC-AFS-03_6 – Specialist heeft al een afspraak op dat tijdstip
        // =====================================================================

        [TestMethod, TestCategory("TC-AFS-03_6_1")]
        public async Task TC_AFS_03_6_1_Medewerker_PlantOpBezetTijdstip_WordtGeweigerd()
        {
            // Arrange
            await AfsData.CreateAppointmentAsync(Jan, DeVries, Jan12_1000);
            var referralsBefore = await AfsData.CountReferralsAsync();
            var plan = await OpenPlanningAsync(Driver, "medewerker.a", Piet);

            // Act
            var offeredInUi = plan.HasSlot(DeVries, Jan12_1000);
            var status = await PostAsync(Driver, Piet, DeVries, Jan12_1000);

            // Assert
            Assert.IsFalse(offeredInUi, "Bezet tijdstip wordt nog aangeboden.");
            Assert.IsTrue(status is 400 or 409 or 422, $"Verwacht weigering, kreeg {status}.");
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual(0, await AfsData.CountAppointmentsOfPatientAsync(Piet));
            Assert.AreEqual(referralsBefore, await AfsData.CountReferralsAsync(), "Er is een wees-verwijzing achtergebleven.");
        }

        [TestMethod, TestCategory("TC-AFS-03_6_2")]
        public async Task TC_AFS_03_6_2_Medewerker_PlantDirectVoorEnNaBestaandeAfspraak()
        {
            // Arrange
            await AfsData.CreateAppointmentAsync(Jan, DeVries, Jan12_1000);

            // Act
            var planPiet = await OpenPlanningAsync(Driver, "medewerker.a", Piet);
            Book(planPiet, DeVries, Jan12_0900);
            var planAnna = await OpenPlanningAsync(Driver, "medewerker.a", Anna);
            Book(planAnna, DeVries, Jan12_1100);

            // Assert
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_0900));
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1100));
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.IsFalse(planAnna.HasMessage && planAnna.Message.Contains(MsgSpecialistBusy));
        }

        // =====================================================================
        // AC-AFS-03_7 – Twee medewerkers boeken tegelijk hetzelfde tijdslot
        // =====================================================================

        [TestMethod, TestCategory("TC-AFS-03_7_1")]
        public async Task TC_AFS_03_7_1_TweeMedewerkers_BoekenParalleelHetzelfdeTijdslot()
        {
            // Arrange
            var patientJan = await AfsData.PatientIdAsync(Jan);
            var patientPiet = await AfsData.PatientIdAsync(Piet);
            var specialistId = await AfsData.SpecialistIdAsync(DeVries);
            using var driverB = ExtraBrowser.Start();
            LoginAs(Driver, "medewerker.a");
            LoginAs(driverB, "medewerker.b");

            for (var run = 1; run <= 10; run++)
            {
                await AfsData.ResetAppointmentsAsync();
                var referralsBefore = await AfsData.CountReferralsAsync();

                // Act
                var taskA = Task.Run(() => BrowserApi.PostAppointment(Driver, patientJan, specialistId, Jan12_1000));
                var taskB = Task.Run(() => BrowserApi.PostAppointment(driverB, patientPiet, specialistId, Jan12_1000));
                var statuses = await Task.WhenAll(taskA, taskB);

                // Assert: één afspraak met één verwijzing, de ander is geweigerd zonder resten
                Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000), $"Run {run}: verwacht precies één afspraak.");
                Assert.AreEqual(1, statuses.Count(s => s is >= 200 and < 300), $"Run {run}: precies één verzoek moet slagen.");
                Assert.AreEqual(1, statuses.Count(s => s is 400 or 409 or 422), $"Run {run}: het andere verzoek moet geweigerd worden.");
                Assert.AreEqual(referralsBefore + 1, await AfsData.CountReferralsAsync(), $"Run {run}: de geweigerde boeking mag geen verwijzing achterlaten.");
            }
        }

        // =====================================================================
        // AC-AFS-03_8 – Afspraak buiten de beschikbaarheid van de specialist
        // =====================================================================

        [TestMethod, TestCategory("TC-AFS-03_8_1")]
        public async Task TC_AFS_03_8_1_Medewerker_PlantOpNietBeschikbareDag_WordtGeweigerd()
        {
            // Arrange
            await AfsData.SetUnavailableAsync(DeVries, Jan12_1000);
            var referralsBefore = await AfsData.CountReferralsAsync();
            var plan = await OpenPlanningAsync(Driver, "medewerker.a", Jan);

            // Act
            var offeredInUi = plan.HasSlot(DeVries, Jan12_1000);
            var status = await PostAsync(Driver, Jan, DeVries, Jan12_1000);

            // Assert
            Assert.IsFalse(offeredInUi, "Niet-beschikbare dag wordt aangeboden.");
            Assert.IsTrue(status is 400 or 409 or 422, $"Verwacht weigering, kreeg {status}.");
            Assert.AreEqual(0, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual(referralsBefore, await AfsData.CountReferralsAsync());
        }

        [TestMethod, TestCategory("TC-AFS-03_8_2")]
        public async Task TC_AFS_03_8_2_Medewerker_PlantDagVoorEnDagNaNietBeschikbareDag()
        {
            // Arrange
            var jan11 = new DateTime(2027, 1, 11, 10, 0, 0);
            var jan13 = new DateTime(2027, 1, 13, 10, 0, 0);
            await AfsData.SetUnavailableAsync(DeVries, Jan12_1000);

            // Act
            var planJan = await OpenPlanningAsync(Driver, "medewerker.a", Jan);
            Book(planJan, DeVries, jan11);
            var planPiet = await OpenPlanningAsync(Driver, "medewerker.a", Piet);
            Book(planPiet, DeVries, jan13);

            // Assert
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, jan11));
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, jan13));

            var planAnna = await OpenPlanningAsync(Driver, "medewerker.a", Anna);
            Assert.IsFalse(planAnna.HasSlot(DeVries, Jan12_1000), "12-01-2027 moet niet boekbaar blijven.");
        }

        [TestMethod, TestCategory("TC-AFS-03_8_3")]
        public async Task TC_AFS_03_8_3_Backend_WeigertAfspraakBuitenBeschikbaarheid()
        {
            // Arrange
            await AfsData.SetUnavailableAsync(DeVries, Jan12_1000);
            var referralsBefore = await AfsData.CountReferralsAsync();
            LoginAs(Driver, "medewerker.a");

            // Act
            var status = await PostAsync(Driver, Jan, DeVries, Jan12_1000);

            // Assert
            Assert.IsTrue(status is 400 or 409 or 422, $"Verwacht weigering, kreeg {status}.");
            Assert.AreEqual(0, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual(referralsBefore, await AfsData.CountReferralsAsync());
        }
    }
}