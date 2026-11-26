using OpenQA.Selenium;
using Server.Tests.Utils;

namespace Server.Tests.PageModels
{
    /// <summary>
    /// Page model van /profiel, onderdeel "Agenda koppeling".
    /// Testid's: ical-section, ical-url, ical-submit, message en ical-disconnect (nog niet in de client).
    /// </summary>
    public class ProfilePage(IWebDriver driver) : BasePage(driver)
    {
        private static readonly By IcalUrl = TestId("ical-url");
        private static readonly By ConnectButton = TestId("ical-submit");

        public ProfilePage Open()
        {
            Navigate("profiel");
            Driver.WaitForElement(TestId("ical-section"));
            return this;
        }

        // =========================================================
        // Koppelen
        // =========================================================

        /// <summary>Vult de iCal-link in (data-testid="ical-url").</summary>
        public ProfilePage TypeIcalUrl(string url)
        {
            Type(IcalUrl, url, "iCal-link");
            return this;
        }

        /// <summary>Knop "Koppelen" (data-testid="ical-submit").</summary>
        public ProfilePage Connect()
        {
            Click(ConnectButton, "Koppelen");
            return this;
        }

        /// <summary>Vult de link in en klikt op "Koppelen".</summary>
        public ProfilePage ConnectWith(string url) => TypeIcalUrl(url).Connect();

        public bool IsConnectButtonEnabled =>
            Driver.FindElement(ConnectButton).Enabled;

        /// <summary>True wanneer de browser de invoer afkeurt omdat het geen URL is (type="url").</summary>
        public bool IsUrlFormatInvalid() =>
            ((IJavaScriptExecutor)Driver).ExecuteScript(
                "return arguments[0].validity.typeMismatch;",
                Driver.WaitForElement(IcalUrl)) is true;

        // =========================================================
        // Ontkoppelen
        // =========================================================

        /// <summary>Knop "Ontkoppelen" (data-testid="ical-disconnect"), alleen zichtbaar bij een gekoppelde agenda.</summary>
        public ProfilePage Disconnect()
        {
            Click(TestId("ical-disconnect"), "Ontkoppelen");
            return this;
        }

        /// <summary>Wacht tot de gekoppelde staat zichtbaar is.</summary>
        public ProfilePage WaitUntilConnected()
        {
            Driver.WaitForElement(TestId("ical-disconnect"));
            return this;
        }

        /// <summary>Wacht tot het koppelformulier weer zichtbaar is.</summary>
        public ProfilePage WaitUntilDisconnected()
        {
            Driver.WaitForElement(IcalUrl);
            return this;
        }

        public bool IsConnected => Exists(TestId("ical-disconnect"));

        public bool HasConnectForm => Exists(IcalUrl);

        // =========================================================
        // Meldingen (data-testid="message", role status/alert)
        // =========================================================

        public void WaitForMessage() => Driver.WaitForElement(TestId("message"));

        public void WaitForSuccessMessage() =>
            Driver.WaitForElement(By.CssSelector("[data-testid='message'][role='status']"));

        public void WaitForErrorMessage() =>
            Driver.WaitForElement(By.CssSelector("[data-testid='message'][role='alert']"));

        public bool HasMessage => Exists(TestId("message"));

        public string Message => Driver.FindElement(TestId("message")).Text;

        public bool HasSuccessMessage =>
            Driver.FindElement(TestId("message")).GetAttribute("role") == "status";

        public bool HasErrorMessage =>
            Driver.FindElement(TestId("message")).GetAttribute("role") == "alert";
    }
}