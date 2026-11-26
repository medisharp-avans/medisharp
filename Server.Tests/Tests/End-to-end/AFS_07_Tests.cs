using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using Server.Models.Enums;
using Server.Tests.PageModels;
using Server.Tests.PageModels.AppointmentPages;
using Server.Tests.TestDb;
using Server.Tests.TestDb.QueryHelpers;
using Server.Tests.Utils;

namespace Server.Tests.EndToEnd
{
    [TestClass]
    public class AFS_07_Tests : EndToEndTestBase
    {
        private const string DeVries = "Dr. De Vries";
        private const string Bakker = "Dr. Bakker";
        private const string Jan = "Jan Jansen";

        private const string MsgNoSpecialist = "Geen beschikbare specialist gevonden.";

        // Aanname: stem de exacte tekst af met het team (AC-AFS-07_4 noemt geen tekst).
        private const string MsgLinkNotFound = "De iCal/ICS-link kon niet worden gevonden";

        /// <summary>
        /// De server plant op dit moment altijd op morgen 10:00 (hardcoded in
        /// PlanningService.SetSpecialistForAppointment). Vervang zodra het tijdstip niet meer vast ligt.
        /// </summary>
        private static DateTime Slot => DateTime.Today.AddDays(1).AddHours(10);

        private TestIcsServer _ics = null!;

        // =========================================================
        // Helpers
        // =========================================================
        protected override Func<DbContext, Task>? GetTestDataInitializer() => TestAFSDataInitializer.SeedAsync;

        [TestInitialize]
        public async Task ArrangeTestDataAsync()
        {
            _ics = new TestIcsServer();

            // Zorg dat alleen Dr. De Vries beschikbaar is.
            await AfsAppointmentsQueries.MakeOnlyAvailableAsync(DeVries, Specialism.KNO, Slot);
        }

        [TestCleanup]
        public void StopIcsServer() => _ics?.Dispose();

        private static void LoginAs(IWebDriver driver, string username)
        {
            // new LoginPage(driver).LoginAs(username);
        }

        private ProfilePage OpenProfileAs(string username)
        {
            LoginAs(Driver, username);
            return new ProfilePage(Driver).Open();
        }

        /// <summary>Koppelt de agenda via de profielpagina en wacht op de gekoppelde staat.</summary>
        private ProfilePage ConnectCalendarAs(string username, string url)
        {
            var profile = OpenProfileAs(username);
            profile.ConnectWith(url);
            profile.WaitUntilConnected();
            return profile;
        }

        /// <summary>Plant als medewerker een afspraak en wacht op de melding.</summary>
        private PlanAppointmentPage BookAsEmployee(string patient)
        {
            LoginAs(Driver, "medewerker.a");

            var plan = new PlanAppointmentPage(Driver).Open();
            plan.FillForm(patient, (int)Specialism.KNO, DeVries);
            plan.Submit();
            plan.WaitForMessage();
            return plan;
        }

        // =========================================================
        // AC-AFS-07_1 – Specialist kan agenda koppelen
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-07_1_1")]
        public async Task TC_AFS_07_1_1_Specialist_KoppeltGeldigeAgenda_AgendaIsGekoppeld()
        {
            // Arrange
            var url = _ics.AddCalendar(Slot.Date.AddHours(14), TimeSpan.FromHours(1));
            var profile = OpenProfileAs("dr.devries");

            Assert.IsFalse(profile.IsConnectButtonEnabled, "Zonder link moet 'Koppelen' uitgeschakeld zijn.");
            Assert.AreEqual(0, await AfsCalendarQueries.CountLinksAsync(DeVries));

            // Act
            profile.TypeIcalUrl(url);
            Assert.IsTrue(profile.IsConnectButtonEnabled, "Met een link moet 'Koppelen' actief zijn.");
            profile.Connect();
            profile.WaitForMessage();

            // Assert
            Assert.IsTrue(profile.HasSuccessMessage, profile.Message);
            Assert.IsTrue(profile.IsConnected);
            Assert.AreEqual(1, await AfsCalendarQueries.CountLinksAsync(DeVries));
            Assert.IsTrue(await AfsCalendarQueries.IsSyncedAsync(DeVries), "LastSyncedUtc moet gevuld zijn.");
            Assert.AreEqual(1, await AfsCalendarQueries.CountEventsAsync(DeVries));

            // De koppeling blijft na herladen bestaan
            var reloaded = new ProfilePage(Driver).Open();
            Assert.IsTrue(reloaded.IsConnected);
        }

        [TestMethod, TestCategory("TC-AFS-07_1_2")]
        [DataRow("-//Google Inc//Google Calendar 70.9054//EN", DisplayName = "Google")]
        [DataRow("-//Microsoft Corporation//Outlook 16.0 MIMEDIR//EN", DisplayName = "Outlook")]
        [DataRow("-//Apple Inc.//iCal 5.0//EN", DisplayName = "Apple")]
        public async Task TC_AFS_07_1_2_Specialist_KoppeltAgendaVanElkeAanbieder_AgendaIsGekoppeld(string prodId)
        {
            // Arrange
            var url = _ics.AddCalendar(Slot.Date.AddHours(14), TimeSpan.FromHours(1), prodId);
            var profile = OpenProfileAs("dr.devries");

            // Act
            profile.ConnectWith(url);
            profile.WaitForMessage();

            // Assert
            Assert.IsTrue(profile.HasSuccessMessage, profile.Message);
            Assert.AreEqual(1, await AfsCalendarQueries.CountLinksAsync(DeVries));
        }

        // =========================================================
        // AC-AFS-07_2 – Privéafspraak blokkeert tijdslot
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-07_2_1")]
        public async Task TC_AFS_07_2_1_Medewerker_PlantOpDagMetPriveafspraak_WordtGeweigerd()
        {
            // Arrange: privéafspraak om 14:00, de boeking is om 10:00. De hele dag moet dus geblokkeerd zijn.
            var url = _ics.AddCalendar(Slot.Date.AddHours(14), TimeSpan.FromHours(1));
            ConnectCalendarAs("dr.devries", url);

            var referralsBefore = await AfsAppointmentsQueries.CountReferralsAsync();

            // Act
            var plan = BookAsEmployee(Jan);

            // Assert
            Assert.IsTrue(plan.HasErrorMessage, plan.Message);
            StringAssert.Contains(plan.Message, MsgNoSpecialist);

            Assert.AreEqual(0, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, Slot));
            Assert.AreEqual(referralsBefore, await AfsAppointmentsQueries.CountReferralsAsync(),
                "Er is een wees-verwijzing achtergebleven.");
        }

        [TestMethod, TestCategory("TC-AFS-07_2_2")]
        public async Task TC_AFS_07_2_2_Medewerker_PlantOpDagZonderPriveafspraak_WordtOpgeslagen()
        {
            // Arrange: de privéafspraak staat een dag later, op de dag zelf is niets geblokkeerd.
            var url = _ics.AddCalendar(Slot.Date.AddDays(1).AddHours(14), TimeSpan.FromHours(1));
            ConnectCalendarAs("dr.devries", url);

            // Act
            var plan = BookAsEmployee(Jan);

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, Slot));
        }

        // =========================================================
        // AC-AFS-07_3 – Specialist kan agenda ontkoppelen
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-07_3_1")]
        public async Task TC_AFS_07_3_1_Specialist_OntkoppeltAgenda_DataIsVerwijderdEnDagIsWeerVrij()
        {
            // Arrange
            var url = _ics.AddCalendar(Slot.Date.AddHours(14), TimeSpan.FromHours(1));
            var profile = ConnectCalendarAs("dr.devries", url);

            Assert.AreEqual(1, await AfsCalendarQueries.CountLinksAsync(DeVries));
            Assert.IsTrue(await AfsCalendarQueries.CountEventsAsync(DeVries) > 0,
                "Na het koppelen moet de agenda gesynchroniseerd zijn.");

            // Act
            profile.Disconnect();
            profile.WaitUntilDisconnected();

            // Assert
            Assert.IsTrue(profile.HasConnectForm);
            Assert.IsFalse(profile.IsConnected);
            Assert.AreEqual(0, await AfsCalendarQueries.CountLinksAsync(DeVries));
            Assert.AreEqual(0, await AfsCalendarQueries.CountEventsAsync(DeVries));

            // De dag is niet langer geblokkeerd
            var plan = BookAsEmployee(Jan);
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, Slot));
        }

        [TestMethod, TestCategory("TC-AFS-07_3_2")]
        public async Task TC_AFS_07_3_2_Specialist_OntkoppeltAgenda_AgendaVanAndereSpecialistBlijftBestaan()
        {
            // Arrange
            ConnectCalendarAs("dr.bakker", _ics.AddCalendar(Slot.Date.AddHours(14), TimeSpan.FromHours(1)));
            var profile = ConnectCalendarAs("dr.devries", _ics.AddCalendar(Slot.Date.AddHours(15), TimeSpan.FromHours(1)));

            var bakkerEventsBefore = await AfsCalendarQueries.CountEventsAsync(Bakker);

            // Act
            profile.Disconnect();
            profile.WaitUntilDisconnected();

            // Assert
            Assert.AreEqual(0, await AfsCalendarQueries.CountLinksAsync(DeVries));
            Assert.AreEqual(0, await AfsCalendarQueries.CountEventsAsync(DeVries));

            Assert.AreEqual(1, await AfsCalendarQueries.CountLinksAsync(Bakker));
            Assert.AreEqual(bakkerEventsBefore, await AfsCalendarQueries.CountEventsAsync(Bakker));
        }

        // =========================================================
        // AC-AFS-07_4 – Ongeldige iCal/ICS-link
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-07_4_1")]
        public async Task TC_AFS_07_4_1_Specialist_KoppeltGeldigeUrlZonderAgenda_KrijgtFoutmelding()
        {
            // Arrange
            var profile = OpenProfileAs("dr.devries");

            // Act
            profile.ConnectWith(_ics.NotACalendarUrl);
            profile.WaitForMessage();

            // Assert
            Assert.IsTrue(profile.HasErrorMessage, profile.Message);
            StringAssert.Contains(profile.Message, MsgLinkNotFound);

            Assert.IsFalse(profile.IsConnected);
            Assert.IsTrue(profile.HasConnectForm, "Het veld blijft beschikbaar voor een nieuwe poging.");
            Assert.AreEqual(0, await AfsCalendarQueries.CountLinksAsync(DeVries));
        }

        [TestMethod, TestCategory("TC-AFS-07_4_2")]
        public async Task TC_AFS_07_4_2_Specialist_KoppeltNietBestaandeUrl_KrijgtFoutmelding()
        {
            // Arrange
            var profile = OpenProfileAs("dr.devries");

            // Act
            profile.ConnectWith(_ics.NotFoundUrl);
            profile.WaitForMessage();

            // Assert
            Assert.IsTrue(profile.HasErrorMessage, profile.Message);
            StringAssert.Contains(profile.Message, MsgLinkNotFound);

            Assert.IsFalse(profile.IsConnected);
            Assert.AreEqual(0, await AfsCalendarQueries.CountLinksAsync(DeVries));
        }

        [TestMethod, TestCategory("TC-AFS-07_4_3")]
        public async Task TC_AFS_07_4_3_Specialist_VultTekstZonderUrlIn_BrowserHoudtInvoerTegen()
        {
            // Arrange
            var profile = OpenProfileAs("dr.devries");

            // Act
            profile.ConnectWith("geen-link");

            // Assert: het formulier wordt niet verstuurd, dus er verschijnt geen melding
            Assert.IsTrue(profile.IsUrlFormatInvalid(), "De browser moet 'geen-link' afkeuren als URL.");
            Assert.IsFalse(profile.HasMessage);
            Assert.IsFalse(profile.IsConnected);
            Assert.AreEqual(0, await AfsCalendarQueries.CountLinksAsync(DeVries));
        }

        [TestMethod, TestCategory("TC-AFS-07_4_4")]
        public async Task TC_AFS_07_4_4_Specialist_LaatVeldLeeg_KoppelenIsUitgeschakeld()
        {
            // Arrange
            var profile = OpenProfileAs("dr.devries");

            // Act + Assert
            Assert.IsFalse(profile.IsConnectButtonEnabled);
            Assert.IsFalse(profile.HasMessage);
            Assert.AreEqual(0, await AfsCalendarQueries.CountLinksAsync(DeVries));
        }
    }
}