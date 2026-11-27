using OpenQA.Selenium;

namespace Server.Tests.PageModels
{
    /// <summary>Side menu of the portal (ids from the existing layout).</summary>
    public class PortalNavigation(IWebDriver driver) : BasePage(driver)
    {
        public void GoToReferrals() => Click(By.Id("ReferralsRoute-link"), "menu Verwijsbrieven");
    }
}
