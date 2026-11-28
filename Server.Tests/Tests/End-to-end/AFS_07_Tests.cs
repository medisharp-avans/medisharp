using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using Server.Models.Enums;
using Server.Tests.PageModels;
using Server.Tests.PageModels.AppointmentPages;
using Server.Tests.TestDb;
using Server.Tests.TestDb.QueryHelpers;
using Server.Tests.Utils;

namespace Server.Tests.Tests.EndToEnd
{
    [TestClass]
    public class AFS_07_Tests : EndToEndTestBase
    {
        private const string DeVries = "Dr. De Vries";
        private const string Bakker = "Dr. Bakker";
        private const string Jan = "Jan Jansen";

        private const string MsgNoSpecialist =
            "Geen beschikbare specialist gevonden binnen de termijn van 3 weken.";

        // Aanname: stem de exacte tekst af met het team (AC-AFS-07_4 noemt geen tekst).
        private const string MsgLinkNotFound = "De iCal/ICS-link kon niet worden gevonden";

        private static readonly TimeZoneInfo DutchTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        private static DateTime WindowStart =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, DutchTimeZone).Date.AddDays(1);

        private static DateTime WindowEnd =>
            WindowStart.AddDays(21);

        /// <summary>
        /// Het eerste tijdslot dat het nieuwe algoritme kiest:
        /// de eerstvolgende werkdag (ma-vr) binnen het 3-weken venster om 09:00.
        /// </summary>
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

        private TestIcsServer _ics = null!;

        // =========================================================
        // Helpers
        // =========================================================
        protected override Func<DbContext, Task>? GetTestDataInitializer() => TestAFSDataInitializer.SeedAsync;

        [TestInitialize]
        public async Task ArrangeTestDataAsync()
        {
            _ics = new TestIcsServer();

            // Zorg dat alleen Dr. De Vries binnen het specialisme KNO vrij is in de 3-wekentermijn.
            await AfsAppointmentsQueries.MakeOnlyAvailableAsync(
                DeVries,
                Specialism.KNO,
                WindowStart,
                WindowEnd);
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
            plan.FillForm(patient, Specialism.KNO, DeVries);
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
            var url = _ics.AddCalendar(FirstSlot.Date.AddHours(14), TimeSpan.FromHours(1));
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
            var url = _ics.AddCalendar(FirstSlot.Date.AddHours(14), TimeSpan.FromHours(1), prodId);
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
        public async Task TC_AFS_07_2_1_Medewerker_PlantOpTijdstipMetPriveafspraak_SchuiftDoorNaarVolgendVrijSlot()
        {
            // Arrange: privéafspraak blokkeert FirstSlot (09:00-10:00); het algoritme moet dus uitwijken naar 10:00.
            var url = _ics.AddCalendar(FirstSlot, TimeSpan.FromHours(1));
            ConnectCalendarAs("dr.devries", url);

            // Act
            var plan = BookAsEmployee(Jan);

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);
            Assert.AreEqual(0, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot),
                "Er mag niet tijdens de privéafspraak (09:00) gepland worden.");
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot.AddHours(1)),
                "De afspraak moet aansluitend om 10:00 zijn ingepland.");
        }

        [TestMethod, TestCategory("TC-AFS-07_2_2")]
        public async Task TC_AFS_07_2_2_Medewerker_PlantOpDagZonderPriveafspraakOpEersteSlot_WordtOpFirstSlotOpgeslagen()
        {
            // Arrange: de privéafspraak staat pas om 14:00, dus FirstSlot (09:00) is vrij.
            var url = _ics.AddCalendar(FirstSlot.Date.AddHours(14), TimeSpan.FromHours(1));
            ConnectCalendarAs("dr.devries", url);

            // Act
            var plan = BookAsEmployee(Jan);

            // Assert
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot));
        }

        // =========================================================
        // AC-AFS-07_3 – Specialist kan agenda ontkoppelen
        // =========================================================

        [TestMethod, TestCategory("TC-AFS-07_3_1")]
        public async Task TC_AFS_07_3_1_Specialist_OntkoppeltAgenda_DataIsVerwijderdEnTijdslotIsWeerVrij()
        {
            // Arrange: privéafspraak blokkeert FirstSlot (09:00-10:00)
            var url = _ics.AddCalendar(FirstSlot, TimeSpan.FromHours(1));
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

            // Na ontkoppelen is FirstSlot (09:00) niet langer geblokkeerd en wordt daar weer direct gepland
            var plan = BookAsEmployee(Jan);
            Assert.IsTrue(plan.HasSuccessMessage, plan.Message);
            Assert.AreEqual(1, await AfsAppointmentsQueries.CountAppointmentsAsync(DeVries, FirstSlot));
        }

        [TestMethod, TestCategory("TC-AFS-07_3_2")]
        [Ignore("Tijdelijk overgeslagen: inloggen als verschillende specialisten is nog niet geïmplementeerd.")]
        public async Task TC_AFS_07_3_2_Specialist_OntkoppeltAgenda_AgendaVanAndereSpecialistBlijftBestaan()
        {
            // Arrange
            ConnectCalendarAs("dr.bakker", _ics.AddCalendar(FirstSlot.Date.AddHours(14), TimeSpan.FromHours(1)));
            var profile = ConnectCalendarAs("dr.devries", _ics.AddCalendar(FirstSlot.Date.AddHours(15), TimeSpan.FromHours(1)));

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