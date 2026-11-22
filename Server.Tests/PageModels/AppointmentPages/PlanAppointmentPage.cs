using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Server.Tests.Utils;

namespace Server.Tests.PageModels.AppointmentPages
{
    /// <summary>Page model van /afspraken (AppointmentsView.vue). Locators zijn data-testid's.</summary>
    public class PlanAppointmentPage(IWebDriver driver)
    {
        private static TimeSpan Timeout => TestSettings.Timeout;

        /// <summary>Eerste dag van de periode (14 dagen) waarin de tests tijdsloten verwachten.</summary>
        private static readonly DateTime DefaultFrom = new(2027, 1, 11);

        private WebDriverWait Wait => new(driver, Timeout);

        /// <summary>
        /// Wacht op een voorwaarde. Bij een time-out staat in de melding op welke pagina de browser stond
        /// en welke tekst zichtbaar was, zodat je ziet of het een foutpagina, een leeg scherm of iets anders is.
        /// </summary>
        private void WaitUntil(Func<IWebDriver, bool> condition, string what)
        {
            try
            {
                Wait.Until(condition);
            }
            catch (WebDriverTimeoutException)
            {
                var body = driver.FindElements(By.TagName("body")).FirstOrDefault()?.Text ?? "(geen body)";
                if (body.Length > 300) body = body[..300] + "…";
                throw new WebDriverTimeoutException(
                    $"Time-out na {Timeout.TotalSeconds} s bij wachten op: {what}. URL: {driver.Url}. " +
                    $"Zichtbare tekst: \"{body}\". Browserconsole: {BrowserConsole()}");
            }
        }

        /// <summary>De laatste waarschuwingen en fouten uit de browserconsole, bijvoorbeeld een CORS-fout of een geweigerde verbinding.</summary>
        private string BrowserConsole()
        {
            try
            {
                var entries = driver.Manage().Logs.GetLog(LogType.Browser)
                    .Where(e => e.Level is LogLevel.Severe or LogLevel.Warning)
                    .TakeLast(5)
                    .Select(e => e.Message);
                var text = string.Join(" | ", entries);
                return text.Length == 0 ? "(geen fouten)" : text;
            }
            catch (Exception)
            {
                return "(niet beschikbaar)";
            }
        }

        private static By TestId(string id) => By.CssSelector($"[data-testid='{id}']");

        private static string SpecialistSlots(string specialist) =>
            $"[data-testid='specialist-slots'][data-specialist='{specialist}']";

        private static string SlotSelector(string specialist, DateTime start) =>
            $"{SpecialistSlots(specialist)} [data-testid='slot'][data-start='{start:yyyy-MM-ddTHH:mm}']";

        /// <summary>Opent de planningspagina voor een patiënt. Er is geen verwijzing nodig: die wordt bij het boeken aangemaakt.</summary>
        public PlanAppointmentPage Open(int patientId)
        {
            driver.Navigate().GoToUrl($"{TestSettings.ClientUrl}/afspraken?patientId={patientId}");
            return this;
        }

        /// <summary>
        /// Kiest het specialisme (standaard KNO), wacht op de behandelcodes en laadt de tijdsloten.
        /// </summary>
        public PlanAppointmentPage OpenTimeSlots(string specialism = "KNO", DateTime? from = null)
        {
            WaitUntil(d => d.FindElements(TestId("patient-name")).Count > 0, "de patiëntnaam (laden van /api/patients/{id})");

            new SelectElement(driver.FindElement(TestId("specialism-select"))).SelectByText(specialism);
            // De behandelcodes van dit specialisme worden daarna opgehaald
            WaitUntil(d => new SelectElement(d.FindElement(TestId("treatment-code-select"))).Options.Count > 0, "de behandelcodes (laden van /api/treatmentcodes)");

            var dateInput = driver.FindElement(TestId("from-date"));
            ((IJavaScriptExecutor)driver).ExecuteScript(
                "arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('input', { bubbles: true }));",
                dateInput, (from ?? DefaultFrom).ToString("yyyy-MM-dd"));

            driver.FindElement(TestId("show-slots")).Click();
            WaitForSlots();
            return this;
        }

        public bool HasSlot(string specialist, DateTime start)
        {
            WaitForSlots();
            return driver.FindElements(By.CssSelector(SlotSelector(specialist, start))).Count > 0;
        }

        public bool HasAnySlotOf(string specialist)
        {
            WaitForSlots();
            return driver.FindElements(By.CssSelector($"{SpecialistSlots(specialist)} [data-testid='slot']")).Count > 0;
        }

        public void SelectSlot(string specialist, DateTime start)
        {
            WaitForSlots();
            var slot = Wait.Until(d => d.FindElements(By.CssSelector(SlotSelector(specialist, start))).FirstOrDefault());
            slot.Click();
        }

        /// <summary>Bevestigt en wacht tot de pagina een melding toont (succes of fout).</summary>
        public void Confirm()
        {
            driver.FindElement(TestId("confirm")).Click();
            WaitForMessage();
        }

        public bool HasMessage => driver.FindElements(TestId("message")).Count > 0;

        public string Message => driver.FindElement(TestId("message")).Text;

        public PlanAppointmentPage WaitForMessage()
        {
            WaitUntil(d => d.FindElements(TestId("message")).Count > 0, "een melding na het bevestigen");
            return this;
        }

        /// <summary>De pagina zet data-loaded op 'false' tijdens het laden en op 'true' als de lijst klaar is.</summary>
        private void WaitForSlots() =>
            WaitUntil(d => d.FindElements(By.CssSelector("[data-testid='slots'][data-loaded='true']")).Count > 0,
                "de tijdsloten (laden van /api/timeslots)");
    }
}