using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Server.Tests.Utils;

namespace Server.Tests.PageModels
{
    /// <summary>Page model van /patienten (PatientsView.vue).</summary>
    public class StaffPatientsPage(IWebDriver driver)
    {
        private WebDriverWait Wait => new(driver, TestSettings.Timeout);

        public StaffPatientsPage Open()
        {
            driver.Navigate().GoToUrl($"{TestSettings.ClientUrl}/patienten");
            Wait.Until(d => d.FindElements(By.CssSelector("[data-testid='patient-search']")).Count > 0);
            return this;
        }

        public StaffPatientsPage Search(string name)
        {
            var input = driver.FindElement(By.CssSelector("[data-testid='patient-search']"));
            input.Clear();
            input.SendKeys(name);
            driver.FindElement(By.CssSelector("[data-testid='patient-search-submit']")).Click();
            Wait.Until(d => d.FindElements(By.CssSelector($"[data-testid='patient-row'][data-patient='{name}']")).Count > 0);
            return this;
        }

        public void StartPlanning(string patient)
        {
            var link = Wait.Until(d => d.FindElements(By.CssSelector(
                $"[data-testid='patient-row'][data-patient='{patient}'] [data-testid='plan-patient']")).FirstOrDefault());
            link.Click();
        }
    }
}