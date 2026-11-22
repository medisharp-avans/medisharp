using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Server.Tests.Utils;

namespace Server.Tests.PageModels
{
    /// <summary>Page model van /verwijsbrieven (ReferralsView.vue).</summary>
    public class StaffReferralsPage(IWebDriver driver)
    {
        private WebDriverWait Wait => new(driver, TestSettings.Timeout);

        public StaffReferralsPage Open()
        {
            driver.Navigate().GoToUrl($"{TestSettings.ClientUrl}/verwijsbrieven");
            Wait.Until(d => d.FindElements(By.CssSelector("[data-testid='referral-row']")).Count > 0);
            return this;
        }

        /// <summary>Opent de planningspagina vanuit de open verwijzing van deze patiënt.</summary>
        public void OpenReferralOf(string patient)
        {
            var link = Wait.Until(d => d.FindElements(By.CssSelector(
                $"[data-testid='referral-row'][data-patient='{patient}'] [data-testid='plan-referral']")).FirstOrDefault());
            link.Click();
        }
    }
}