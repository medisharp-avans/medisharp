using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using Server.Tests.Helpers;
using Server.Tests.PageModels;
using Server.Tests.PageModels.AppointmentPages;

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

        private PlanAppointmentPage OpenPlanningAsStaff(IWebDriver driver, string staffUser, string patient)
        {
            LoginAs(driver, staffUser);
            new StaffReferralsPage(driver).Open().OpenReferralOf(patient);
            return new PlanAppointmentPage(driver).OpenTimeSlots();
        }

        private void Book(PlanAppointmentPage plan, string specialist, DateTime start)
        {
            plan.SelectSlot(specialist, start);
            plan.Confirm();
        }

        /// <summary>
        /// Stuurt een POST naar /api/appointments. De namen worden hier omgezet naar ids,
        /// want de DTO verwacht PatientId en SpecialistId.
        /// </summary>
        private static async Task<long> PostAsync(
            IWebDriver driver, string patient, string specialist, DateTime start, int? referralId = null)
        {
            var patientId = await AfsData.PatientIdAsync(patient);
            var specialistId = await AfsData.SpecialistIdAsync(specialist);
            return BrowserApi.PostAppointment(driver, patientId, specialistId, start, referralId);
        }

        // =====================================================================
        // AC-AFS-03_1 – Medewerker plant een afspraak bij een beschikbare specialist
        // =====================================================================

        [TestMethod, TestCategory("TC-AFS-03_1_1")]
        public async Task TC_AFS_03_1_1_Medewerker_PlantAfspraakMetGeldigeVerwijzing_AfspraakIsOpgeslagenEnZichtbaarVoorPatient()
        {
            // Arrange
            var referralId = await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            var plan = OpenPlanningAsStaff(Driver, "medewerker.a", Jan);

            // Act
            Book(plan, DeVries, Jan12_1000);

            // Assert: opgeslagen, status, tijdslot bezet
            Assert.AreEqual("Ingepland", await AfsData.ReferralStatusAsync(referralId));
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual(1, await AfsData.CountAppointmentsOfPatientAsync(Jan));

            // Tijdslot niet meer vrij voor anderen
            await AfsData.CreateOpenReferralAsync(Piet, "KNO");
            var planPiet = OpenPlanningAsStaff(Driver, "medewerker.a", Piet);
            Assert.IsFalse(planPiet.HasSlot(DeVries, Jan12_1000), "Het geboekte tijdslot wordt nog aangeboden.");
        }

        [TestMethod, TestCategory("TC-AFS-03_1_2")]
        public async Task TC_AFS_03_1_2_Medewerker_PlantTijdslotDirectNaBestaandeAfspraak_WordtOpgeslagenZonderConflict()
        {
            // Arrange: afspraak van 09:00 duurt 60 minuten (KNO-code), dus 10:00 sluit direct aan
            var referralPiet = await AfsData.CreateOpenReferralAsync(Piet, "KNO");
            await AfsData.CreateAppointmentAsync(Piet, DeVries, Jan12_0900, referralPiet);
            var referralJan = await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            var plan = OpenPlanningAsStaff(Driver, "medewerker.a", Jan);

            // Act
            Book(plan, DeVries, Jan12_1000);

            // Assert
            Assert.AreEqual("Ingepland", await AfsData.ReferralStatusAsync(referralJan));
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_0900), "Bestaande afspraak is gewijzigd.");
            Assert.IsFalse(plan.HasMessage && plan.Message.Contains(MsgSpecialistBusy));
        }

        [TestMethod, TestCategory("TC-AFS-03_1_3")]
        [Ignore("Wacht op login: rol (huisarts/patiënt) kan nog niet worden aangenomen.")]
        public async Task TC_AFS_03_1_3_Huisarts_KanGeenAfspraakPlannen_InterfaceEnBackendWeigeren()
        {
            // Arrange
            var referralId = await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            LoginAs(Driver, "huisarts.visser");

            // Act
            var hasUiFunction = new PatientAppointmentsPage(Driver).Open().HasPlanFunction;
            var status = await PostAsync(Driver, Jan, DeVries, Jan12_1000, referralId);

            // Assert
            Assert.IsFalse(hasUiFunction, "Huisarts ziet een functie om te plannen.");
            Assert.AreEqual(403, status);
            Assert.AreEqual(0, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual("Open", await AfsData.ReferralStatusAsync(referralId));
        }

        // =====================================================================
        // AC-AFS-03_2 – Een afspraak zonder verwijzing is toegestaan
        // =====================================================================

        [TestMethod, TestCategory("TC-AFS-03_2_1")]
        public async Task TC_AFS_03_2_1_Medewerker_PlantZonderVerwijzing_WordtToegestaan()
        {
            // Arrange
            await AfsData.DeleteReferralsAsync(Piet);
            LoginAs(Driver, "medewerker.a");
            var patients = new StaffPatientsPage(Driver).Open().Search(Piet);

            // Act
            patients.StartPlanning(Piet);
            var plan = new PlanAppointmentPage(Driver);
            plan.OpenTimeSlots();

            // Geen voorwaardelijke boeking meer: het tijdslot moet gewoon aangeboden worden
            Assert.IsTrue(plan.HasSlot(DeVries, Jan12_1000), "Tijdslot wordt niet aangeboden voor een patiënt zonder verwijzing.");
            Book(plan, DeVries, Jan12_1000);

            // Assert
            Assert.AreEqual(1, await AfsData.CountAppointmentsOfPatientAsync(Piet));
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual(1, await AfsData.CountAppointmentsWithoutReferralAsync(Piet), "Afspraak hoort geen verwijzing te hebben.");
        }

        [TestMethod, TestCategory("TC-AFS-03_2_2")]
        public async Task TC_AFS_03_2_2_Medewerker_PlantVervolgafspraak_BestaandeVerwijzingBlijftIngepland()
        {
            // Arrange: Jan heeft al een afspraak met status 'Ingepland'.
            // Een verwijzing kan maar één afspraak hebben (1-op-1), dus de vervolgafspraak heeft geen verwijzing.
            var referralId = await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            await AfsData.CreateAppointmentAsync(Jan, DeVries, Jan12_1000, referralId);
            LoginAs(Driver, "medewerker.a");
            var patients = new StaffPatientsPage(Driver).Open().Search(Jan);

            // Act
            patients.StartPlanning(Jan);
            var plan = new PlanAppointmentPage(Driver);
            plan.OpenTimeSlots();

            Assert.IsTrue(plan.HasSlot(DeVries, Jan19_1100), "Tijdslot voor de vervolgafspraak wordt niet aangeboden.");
            Book(plan, DeVries, Jan19_1100);

            // Assert
            Assert.AreEqual("Ingepland", await AfsData.ReferralStatusAsync(referralId));
            Assert.AreEqual(2, await AfsData.CountAppointmentsOfPatientAsync(Jan));
            Assert.AreEqual(1, await AfsData.CountAppointmentsWithoutReferralAsync(Jan));
        }

        [TestMethod, TestCategory("TC-AFS-03_2_3")]
        public async Task TC_AFS_03_2_3_Backend_AccepteertAfspraakZonderVerwijzing()
        {
            // Arrange
            await AfsData.DeleteReferralsAsync(Piet);
            LoginAs(Driver, "medewerker.a");

            // Act: referralId null = geen verwijzing
            var status = await PostAsync(Driver, Piet, DeVries, Jan12_1000, referralId: null);

            // Assert
            Assert.IsTrue(status is >= 200 and < 300, $"Verwacht succesvolle boeking (2xx), kreeg {status}.");
            Assert.AreEqual(1, await AfsData.CountAppointmentsOfPatientAsync(Piet));
            Assert.AreEqual(1, await AfsData.CountAppointmentsWithoutReferralAsync(Piet), "Afspraak hoort geen verwijzing te hebben.");
        }

        // =====================================================================
        // AC-AFS-03_3 – Tijdslot is ondertussen door iemand anders gekozen
        // =====================================================================

        [TestMethod, TestCategory("TC-AFS-03_3_1")]
        public async Task TC_AFS_03_3_1_Medewerker_BevestigtTijdslotDatOndertussenIsGeboekt_KrijgtMelding()
        {
            // Arrange
            var referralJan = await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            await AfsData.CreateOpenReferralAsync(Piet, "KNO");
            using var driverB = ExtraBrowser.Start();
            var planA = OpenPlanningAsStaff(Driver, "medewerker.a", Jan);
            var planB = OpenPlanningAsStaff(driverB, "medewerker.b", Piet);
            planA.SelectSlot(DeVries, Jan12_1000);
            planB.SelectSlot(DeVries, Jan12_1000);
            planB.Confirm();

            // Act
            planA.Confirm();

            // Assert
            StringAssert.Contains(planA.WaitForMessage().Message, MsgSlotTaken);
            Assert.IsFalse(planA.HasSlot(DeVries, Jan12_1000), "Bezet tijdslot staat nog in de lijst.");
            Assert.IsTrue(planA.HasSlot(DeVries, Jan12_1100), "Overige vrije tijdsloten worden niet getoond.");
            Assert.AreEqual("Open", await AfsData.ReferralStatusAsync(referralJan));
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual(0, await AfsData.CountAppointmentsOfPatientAsync(Jan));
        }

        [TestMethod, TestCategory("TC-AFS-03_3_2")]
        public async Task TC_AFS_03_3_2_Medewerker_KiestAlternatiefTijdslotNaMislukteBoeking()
        {
            // Arrange
            var referralJan = await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            var referralPiet = await AfsData.CreateOpenReferralAsync(Piet, "KNO");
            await AfsData.CreateAppointmentAsync(Piet, DeVries, Jan12_1000, referralPiet);
            var plan = OpenPlanningAsStaff(Driver, "medewerker.a", Jan);
            Assert.IsFalse(plan.HasSlot(DeVries, Jan12_1000));

            // Act
            Book(plan, DeVries, Jan12_1100);

            // Assert
            Assert.AreEqual("Ingepland", await AfsData.ReferralStatusAsync(referralJan));
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1100));
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
        }

        // =====================================================================
        // AC-AFS-03_4 – Alleen tijdsloten van het gekozen specialisme
        // =====================================================================

        [TestMethod, TestCategory("TC-AFS-03_4_1")]
        public async Task TC_AFS_03_4_1_Tijdsloten_ToonUitsluitendSpecialistenVanHetVerwezenSpecialisme()
        {
            // Arrange
            await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            LoginAs(Driver, "medewerker.a");
            new StaffReferralsPage(Driver).Open().OpenReferralOf(Jan);
            var plan = new PlanAppointmentPage(Driver);

            // Act
            plan.OpenTimeSlots();

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
        public async Task TC_AFS_03_5_1_Patient_HeeftGeenFunctieOmZelfAfspraakTePlannen()
        {
            // Arrange
            await AfsData.CreateOpenReferralAsync(Jan, "KNO");
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
            var referralId = await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            LoginAs(Driver, "jan.jansen");

            // Act
            var status = await PostAsync(Driver, Jan, DeVries, Jan12_1000, referralId);

            // Assert
            Assert.AreEqual(403, status);
            Assert.AreEqual(0, await AfsData.CountAppointmentsOfPatientAsync(Jan));
            Assert.AreEqual("Open", await AfsData.ReferralStatusAsync(referralId));
            Assert.AreEqual(0, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
        }

        // =====================================================================
        // AC-AFS-03_6 – Specialist heeft al een afspraak op dat tijdstip
        // =====================================================================

        [TestMethod, TestCategory("TC-AFS-03_6_1")]
        public async Task TC_AFS_03_6_1_Medewerker_PlantOpBezetTijdstip_WordtGeweigerd()
        {
            // Arrange
            var referralJan = await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            await AfsData.CreateAppointmentAsync(Jan, DeVries, Jan12_1000, referralJan);
            var referralPiet = await AfsData.CreateOpenReferralAsync(Piet, "KNO");
            var plan = OpenPlanningAsStaff(Driver, "medewerker.a", Piet);

            // Act
            var offeredInUi = plan.HasSlot(DeVries, Jan12_1000);
            var status = await PostAsync(Driver, Piet, DeVries, Jan12_1000, referralPiet);

            // Assert
            Assert.IsFalse(offeredInUi, "Bezet tijdstip wordt nog aangeboden.");
            Assert.IsTrue(status is 400 or 409 or 422, $"Verwacht weigering, kreeg {status}.");
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual("Open", await AfsData.ReferralStatusAsync(referralPiet));
        }

        [TestMethod, TestCategory("TC-AFS-03_6_2")]
        public async Task TC_AFS_03_6_2_Medewerker_PlantDirectVoorEnNaBestaandeAfspraak()
        {
            // Arrange
            var referralJan = await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            await AfsData.CreateAppointmentAsync(Jan, DeVries, Jan12_1000, referralJan);
            await AfsData.CreateOpenReferralAsync(Piet, "KNO");
            await AfsData.CreateOpenReferralAsync(Anna, "KNO");

            // Act
            var planPiet = OpenPlanningAsStaff(Driver, "medewerker.a", Piet);
            Book(planPiet, DeVries, Jan12_0900);
            var planAnna = OpenPlanningAsStaff(Driver, "medewerker.a", Anna);
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
            var referralJan = await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            var referralPiet = await AfsData.CreateOpenReferralAsync(Piet, "KNO");
            var patientJan = await AfsData.PatientIdAsync(Jan);
            var patientPiet = await AfsData.PatientIdAsync(Piet);
            var specialistId = await AfsData.SpecialistIdAsync(DeVries);
            using var driverB = ExtraBrowser.Start();
            LoginAs(Driver, "medewerker.a");
            LoginAs(driverB, "medewerker.b");

            for (var run = 1; run <= 10; run++)
            {
                await AfsData.ResetAppointmentsAsync(referralJan, referralPiet);

                // Act
                var taskA = Task.Run(() => BrowserApi.PostAppointment(Driver, patientJan, specialistId, Jan12_1000, referralJan));
                var taskB = Task.Run(() => BrowserApi.PostAppointment(driverB, patientPiet, specialistId, Jan12_1000, referralPiet));
                var statuses = await Task.WhenAll(taskA, taskB);

                // Assert
                Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000), $"Run {run}: verwacht precies één afspraak.");
                Assert.AreEqual(1, statuses.Count(s => s is >= 200 and < 300), $"Run {run}: precies één verzoek moet slagen.");
                Assert.AreEqual(1, statuses.Count(s => s is 400 or 409 or 422), $"Run {run}: het andere verzoek moet geweigerd worden.");
            }
        }

        // =====================================================================
        // AC-AFS-03_8 – Afspraak buiten de beschikbaarheid van de specialist
        // =====================================================================

        [TestMethod, TestCategory("TC-AFS-03_8_1")]
        public async Task TC_AFS_03_8_1_Medewerker_PlantOpNietBeschikbareDag_WordtGeweigerd()
        {
            // Arrange
            var referralId = await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            await AfsData.SetUnavailableAsync(DeVries, Jan12_1000);
            var plan = OpenPlanningAsStaff(Driver, "medewerker.a", Jan);

            // Act
            var offeredInUi = plan.HasSlot(DeVries, Jan12_1000);
            var status = await PostAsync(Driver, Jan, DeVries, Jan12_1000, referralId);

            // Assert
            Assert.IsFalse(offeredInUi, "Niet-beschikbare dag wordt aangeboden.");
            Assert.IsTrue(status is 400 or 409 or 422, $"Verwacht weigering, kreeg {status}.");
            Assert.AreEqual(0, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual("Open", await AfsData.ReferralStatusAsync(referralId));
        }

        [TestMethod, TestCategory("TC-AFS-03_8_2")]
        public async Task TC_AFS_03_8_2_Medewerker_PlantDagVoorEnDagNaNietBeschikbareDag()
        {
            // Arrange
            var jan11 = new DateTime(2027, 1, 11, 10, 0, 0);
            var jan13 = new DateTime(2027, 1, 13, 10, 0, 0);
            var referralJan = await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            var referralPiet = await AfsData.CreateOpenReferralAsync(Piet, "KNO");
            await AfsData.SetUnavailableAsync(DeVries, Jan12_1000);

            // Act
            var planJan = OpenPlanningAsStaff(Driver, "medewerker.a", Jan);
            Book(planJan, DeVries, jan11);
            var planPiet = OpenPlanningAsStaff(Driver, "medewerker.a", Piet);
            Book(planPiet, DeVries, jan13);

            // Assert
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, jan11));
            Assert.AreEqual(1, await AfsData.CountAppointmentsAsync(DeVries, jan13));
            Assert.AreEqual("Ingepland", await AfsData.ReferralStatusAsync(referralJan));
            Assert.AreEqual("Ingepland", await AfsData.ReferralStatusAsync(referralPiet));

            await AfsData.CreateOpenReferralAsync(Anna, "KNO");
            var planAnna = OpenPlanningAsStaff(Driver, "medewerker.a", Anna);
            Assert.IsFalse(planAnna.HasSlot(DeVries, Jan12_1000), "12-01-2027 moet niet boekbaar blijven.");
        }

        [TestMethod, TestCategory("TC-AFS-03_8_3")]
        public async Task TC_AFS_03_8_3_Backend_WeigertAfspraakBuitenBeschikbaarheid()
        {
            // Arrange
            var referralId = await AfsData.CreateOpenReferralAsync(Jan, "KNO");
            await AfsData.SetUnavailableAsync(DeVries, Jan12_1000);
            LoginAs(Driver, "medewerker.a");

            // Act
            var status = await PostAsync(Driver, Jan, DeVries, Jan12_1000, referralId);

            // Assert
            Assert.IsTrue(status is 400 or 409 or 422, $"Verwacht weigering, kreeg {status}.");
            Assert.AreEqual(0, await AfsData.CountAppointmentsAsync(DeVries, Jan12_1000));
            Assert.AreEqual("Open", await AfsData.ReferralStatusAsync(referralId));
        }
    }
}