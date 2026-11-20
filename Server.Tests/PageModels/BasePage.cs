using OpenQA.Selenium;
using Server.Tests.Utils;

namespace Server.Tests.PageModels
{
    /// <summary>
    /// Base of all page models: what can be done on any page of the client.
    /// Put the elements of one page in its own page model that inherits from this class.
    /// </summary>
    public abstract class BasePage(IWebDriver driver)
    {
        protected IWebDriver Driver { get; } = driver;

        /// <summary>Opens a path of the client, for example "patients".</summary>
        public void Navigate(string path = "")
        {
            TestLog.Step($"Open /{path.TrimStart('/')}");
            Driver.Navigate().GoToUrl($"{TestSettings.ClientUrl}/{path.TrimStart('/')}");
        }

        /// <summary>Turns off the browser's own form validation, to test the validation of the app itself.</summary>
        public void DisableBrowserValidation() =>
            ((IJavaScriptExecutor)Driver).ExecuteScript("document.querySelectorAll('form').forEach(f => f.noValidate = true);");

        /// <summary>True when the browser blocks the form because this required field is empty.</summary>
        public bool IsRequiredFieldEmpty(By field)
        {
            var element = Driver.WaitForElement(field);
            return ((IJavaScriptExecutor)Driver).ExecuteScript("return arguments[0].validity.valueMissing;", element) is true;
        }
    }
}
