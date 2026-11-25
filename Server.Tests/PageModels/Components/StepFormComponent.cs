using OpenQA.Selenium;
using Server.Tests.Utils;

namespace Server.Tests.PageModels
{
    /// <summary>
    /// Page model for the StepForm component (wizard with progress bar).
    /// Use it inside the page model of every form that is built with StepForm.
    /// </summary>
    public class StepFormComponent(IWebDriver driver) : BasePage(driver)
    {
        private const string IndicatorPrefix = "step-indicator-";

        /// <summary>The id of the step the user is on, for example "care".</summary>
        public string CurrentStepId
        {
            get
            {
                var current = Driver.FindElement(
                    By.CssSelector($"[data-testid^='{IndicatorPrefix}'][data-state='current']"));

                return current.GetDomAttribute("data-testid")![IndicatorPrefix.Length..];
            }
        }

        /// <summary>Only the fields of the current step are in the page.</summary>
        public bool IsOnStep(string stepId) => Exists(TestId($"step-{stepId}"));

        public void WaitForStep(string stepId) =>
            Driver.WaitForElement(TestId($"step-{stepId}"));

        /// <summary>Clicks "Volgende" and waits for the next step. Stays on the same step when the browser blocks it.</summary>
        public void Next(string expectedStepId)
        {
            Click(TestId("step-next"), "Volgende");
            WaitForStep(expectedStepId);
        }

        public void Previous(string expectedStepId)
        {
            Click(TestId("step-previous"), "Vorige");
            WaitForStep(expectedStepId);
        }

        /// <summary>Goes back to a completed step via the progress bar.</summary>
        public void GoToCompletedStep(string stepId)
        {
            Click(
                By.CssSelector($"[data-testid='{IndicatorPrefix}{stepId}'] button"),
                $"stap {stepId} in de voortgangsbalk");
            WaitForStep(stepId);
        }
    }
}