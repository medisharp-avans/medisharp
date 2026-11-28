using OpenQA.Selenium;
using Server.Tests.Utils;
using OpenQA.Selenium.Support.UI;

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

        /// <summary>Locator for an element with this data-testid.</summary>
        protected static By TestId(string id) => By.CssSelector($"[data-testid='{id}']");

        /// <summary>Locator for all elements whose data-testid starts with this prefix.</summary>
        protected static By TestIdStartsWith(string prefix) => By.CssSelector($"[data-testid^='{prefix}']");

        /// <summary>True when the element is on the page right now (no waiting).</summary>
        protected bool Exists(By by) => Driver.FindElements(by).Count > 0;

        /// <summary>Waits for the element and clicks it.</summary>
        protected void Click(By by, string? description = null)
        {
            TestLog.Step($"Click {description ?? by.ToString()}");
            Driver.WaitForElement(by).Click();
        }

        /// <summary>Waits for the field, clears it and types the text.</summary>
        protected void Type(By by, string text, string? description = null)
        {
            TestLog.Step($"Type '{text}' in {description ?? by.ToString()}");
            var element = Driver.WaitForElement(by);
            element.Clear();
            element.SendKeys(text);
        }

        /// <summary>Selects an option in a dropdown by its value.</summary>
        protected void Select(By by, string value, string? description = null)
        {
            TestLog.Step($"Select '{value}' in {description ?? by.ToString()}");

            var element = Driver.WaitForElement(by);
            var dropdown = new SelectElement(element);

            dropdown.SelectByValue(value);
        }

        /// <summary>
        /// Types into a search field with a datalist (DatalistInput.vue). The options are loaded from
        /// the API, so this first waits until the list contains the text. Text that is not an option
        /// makes the form refuse to continue to the next step.
        /// </summary>
        protected void TypeInDatalist(By by, string text, string? description = null)
        {
            var element = Driver.WaitForElement(by);
            var listId = element.GetDomAttribute("list")
                ?? throw new InvalidOperationException($"{description ?? by.ToString()} has no datalist.");

            new WebDriverWait(Driver, TimeSpan.FromSeconds(10))
            {
                Message = $"'{text}' did not appear in the options of {description ?? by.ToString()}."
            }.Until(d => d.FindElements(By.CssSelector($"#{listId} option"))
                          .Any(o => o.GetDomAttribute("value") == text));

            Type(by, text, description);

            // Closes the browser's suggestion popup so it cannot cover the buttons below.
            element.SendKeys(Keys.Escape);
        }
    }
}