using OpenQA.Selenium;
using Server.Tests.Utils;

namespace Server.Tests.PageModels.AppointmentPages
{
    public class PlanAppointmentPage(IWebDriver driver) : BasePage(driver)
    {
        private readonly StepFormComponent _steps = new(driver);

        public PlanAppointmentPage Open()
        {
            Navigate("afspraken/nieuw");
            _steps.WaitForStep("patient");
            return this;
        }

        // =========================================================
        // Navigatie tussen de stappen
        // =========================================================

        public string CurrentStepId => _steps.CurrentStepId;

        public PlanAppointmentPage NextStep(string expectedStepId)
        {
            _steps.Next(expectedStepId);
            return this;
        }

        public PlanAppointmentPage PreviousStep(string expectedStepId)
        {
            _steps.Previous(expectedStepId);
            return this;
        }

        // =========================================================
        // Eén methode per stap (vullen, niet doorgaan)
        // =========================================================

        /// <summary>Stap "patient": patiënt (op naam) en specialisme.</summary>
        public PlanAppointmentPage FillPatientStep(string patient, int specialism)
        {
            TypeInDatalist(TestId("patient-id"), patient, "patiënt");
            Select(TestId("specialism"), specialism.ToString(), "specialisme");
            return this;
        }

        /// <summary>Stap "care": behandelcode en optioneel een voorkeursspecialist (op naam).</summary>
        public PlanAppointmentPage FillCareStep(
            string treatmentCode = "KNO002",
            string? preferredSpecialist = null)
        {
            if (!string.IsNullOrWhiteSpace(preferredSpecialist))
            {
                TypeInDatalist(TestId("specialist-id"), preferredSpecialist, "voorkeursspecialist");
            }

            TypeInDatalist(TestId("treatment-code"), treatmentCode, "behandelcode");
            return this;
        }

        /// <summary>Stap "details": reden, urgentie en optioneel een vragenlijst.</summary>
        public PlanAppointmentPage FillDetailsStep(
            string reason = "Testafspraak",
            string urgency = "Regulier",
            string? questionnaire = null)
        {
            Type(TestId("reason"), reason, "reden");
            Select(TestId("urgency"), urgency, "urgentie");

            if (!string.IsNullOrWhiteSpace(questionnaire))
            {
                Type(TestId("questionnaire"), questionnaire, "vragenlijst");
            }

            return this;
        }

        /// <summary>Vult alle stappen in en stopt op de controle-stap. Daarna kun je Submit() aanroepen.</summary>
        public PlanAppointmentPage FillForm(
            string patient,
            int specialism,
            string? preferredSpecialist = null,
            string treatmentCode = "KNO002",
            string reason = "Testafspraak",
            string urgency = "Regulier",
            string? questionnaire = null)
        {
            FillPatientStep(patient, specialism).NextStep("care");
            FillCareStep(treatmentCode, preferredSpecialist).NextStep("details");
            FillDetailsStep(reason, urgency, questionnaire).NextStep("review");
            return this;
        }

        // =========================================================
        // Controle-stap
        // =========================================================

        /// <summary>De waarde in het overzicht, bijvoorbeeld ReviewValue("patient").</summary>
        public string ReviewValue(string field) =>
            Driver.WaitForElement(TestId($"summary-{field}")).Text;

        /// <summary>Klikt op "Wijzig" bij een veld in het overzicht en wacht op de bijbehorende stap.</summary>
        public PlanAppointmentPage EditFromReview(string field, string expectedStepId)
        {
            Click(TestId($"summary-edit-{field}"), $"Wijzig {field}");
            _steps.WaitForStep(expectedStepId);
            return this;
        }

        /// <summary>Alleen mogelijk op de controle-stap.</summary>
        public PlanAppointmentPage Submit()
        {
            Click(TestId("submit"), "Afspraak aanmaken");
            return this;
        }

        // =========================================================
        // Meldingen
        // =========================================================

        public void WaitForMessage()
        {
            Driver.WaitForElement(TestId("message"));
        }

        /// <summary>
        /// Wacht op een succesmelding. Gebruik dit na een tweede poging op dezelfde pagina,
        /// anders kan de oude foutmelding nog even in beeld staan.
        /// </summary>
        public void WaitForSuccessMessage() =>
            Driver.WaitForElement(By.CssSelector("[data-testid='message'][role='status']"));

        public void WaitForErrorMessage() =>
            Driver.WaitForElement(By.CssSelector("[data-testid='message'][role='alert']"));

        public bool HasMessage => Exists(TestId("message"));

        public string Message =>
            Driver.FindElement(TestId("message")).Text;

        public bool HasSuccessMessage =>
            Driver.FindElement(TestId("message"))
                .GetAttribute("role") == "status";

        public bool HasErrorMessage =>
            Driver.FindElement(TestId("message"))
                .GetAttribute("role") == "alert";

        /// <summary>Alleen de velden van de huidige stap staan in de pagina.</summary>
        public bool HasField(string id) => Exists(TestId(id));
    }
}