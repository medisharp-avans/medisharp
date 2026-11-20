using OpenQA.Selenium;
using Server.Tests.Utils;

namespace Server.Tests.PageModels.AppointmentPages
{
    /// <summary>Referrals overview (/verwijsbrieven) for staff.</summary>
    public class StaffReferralsPage(IWebDriver driver) : BasePage(driver)
    {
        private static readonly By Status = TestId("referral-status");

        public StaffReferralsPage Open()
        {
            Navigate("verwijsbrieven");
            return this;
        }

        public void OpenReferralOf(string patientName) =>
            Click(By.XPath($"//*[@data-testid='referral-row'][contains(.,'{patientName}')]//*[@data-testid='open-referral']"),
                $"referral of {patientName}");

        public string ReferralStatus => Driver.WaitForElement(Status).Text.Trim();
    }
}