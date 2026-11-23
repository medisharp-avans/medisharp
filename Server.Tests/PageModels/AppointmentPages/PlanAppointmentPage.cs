
using OpenQA.Selenium;
using Server.Tests.Utils;

namespace Server.Tests.PageModels.AppointmentPages
{
    public class PlanAppointmentPage(IWebDriver driver) : BasePage(driver)
    {
        public PlanAppointmentPage Open()
        {
            Navigate("afspraken");
            Driver.WaitForElement(TestId("submit"));
            return this;
        }

        public PlanAppointmentPage FillForm(
            int patientId,
            int specialistId,
            DateTime start,
            int generalPractitionerId,
            int specialism,
            string treatmentCode = "KNO002",
            string reason = "Testafspraak",
            string urgency = "Regulier",
            int? preferredSpecialistId = null,
            string? questionnaire = null)
        {
            Type(TestId("patient-id"), patientId.ToString(), "patiënt ID");
            Type(TestId("specialist-id"), specialistId.ToString(), "specialist ID");

            // Een datetime-local veld vullen we via JavaScript.
            var startInput = Driver.WaitForElement(TestId("start-time"));

            ((IJavaScriptExecutor)Driver).ExecuteScript(
                "arguments[0].value = arguments[1];" +
                "arguments[0].dispatchEvent(new Event('input', { bubbles: true }));" +
                "arguments[0].dispatchEvent(new Event('change', { bubbles: true }));",
                startInput,
                start.ToString("yyyy-MM-ddTHH:mm"));

            Type(TestId("general-practitioner-id"),
                generalPractitionerId.ToString(), "huisarts ID");

            Type(TestId("specialism"), specialism.ToString(), "specialisme");
            Type(TestId("reason"), reason, "reden");
            Select(TestId("urgency"), urgency, "urgentie");
            Type(TestId("treatment-code"), treatmentCode, "behandelcode");

            if (preferredSpecialistId.HasValue)
            {
                Type(TestId("preferred-specialist-id"),
                    preferredSpecialistId.Value.ToString(),
                    "voorkeursspecialist ID");
            }

            if (!string.IsNullOrWhiteSpace(questionnaire))
            {
                Type(TestId("questionnaire"), questionnaire, "vragenlijst");
            }

            return this;
        }

        public PlanAppointmentPage Submit()
        {
            Click(TestId("submit"), "Afspraak opslaan");
            return this;
        }

        public void WaitForMessage()
        {
            Driver.WaitForElement(TestId("message"));
        }

        public bool HasMessage => Exists(TestId("message"));

        public string Message =>
            Driver.FindElement(TestId("message")).Text;

        public bool HasSuccessMessage =>
            Driver.FindElement(TestId("message"))
                .GetAttribute("role") == "status";

        public bool HasErrorMessage =>
            Driver.FindElement(TestId("message"))
                .GetAttribute("role") == "alert";

        public bool HasField(string id) => Exists(TestId(id));
    }
}
