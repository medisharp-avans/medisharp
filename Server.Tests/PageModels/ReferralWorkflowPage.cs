using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Server.Tests.Utils;

namespace Server.Tests.PageModels;

/// <summary>Exercises the referral form and the hospital's open-referral list.</summary>
public class ReferralWorkflowPage(IWebDriver driver) : BasePage(driver)
{
    public void CreateReferral(string bsn, string name, string specialty, string treatmentCode)
    {
        Click(By.LinkText("Nieuwe verwijzing"));
        Type(By.Id("patient-bsn"), bsn);
        Type(By.Id("patient-name"), name);
        Type(By.Id("patient-postal-code"), "1234 AB");
        Type(By.Id("patient-house-number"), "12A");
        Type(By.Id("patient-email"), "patient-afs01a@example.test");

        SelectWhenAvailable("referral-specialty", specialty);
        SelectWhenAvailable("referral-treatment", treatmentCode);
        Type(By.Id("referral-reason"), "Fictieve oorpijn. Kan een specialist de oorzaak beoordelen?");
        Type(By.Id("referral-medical-information"), "Fictieve bevinding uit het onderzoek.");
        new SelectElement(Driver.WaitForElement(By.Id("referral-medication"))).SelectByText("Geen medicatie");
        new SelectElement(Driver.WaitForElement(By.Id("referral-allergies"))).SelectByText("Geen bekende allergieën");
        Click(By.CssSelector("form button[type='submit']"), "Verwijzing opslaan");

        Driver.WaitForElement(By.XPath("//*[@role='status' and contains(., 'opgeslagen met status Open')]"));
    }

    public string[] FindOpenReferral(string bsn)
    {
        Click(By.LinkText("Open verwijzingen"));
        var row = Driver.WaitForElement(By.XPath($"//tbody/tr[td[last()][normalize-space(.)='{bsn}']]"));
        return row.FindElements(By.TagName("td")).Select(cell => cell.Text).ToArray();
    }

    public void Logout()
    {
        Click(By.Id("profile-button"), "Profielmenu");
        Click(By.Id("profile-menu-logout"), "Uitloggen");
        Driver.WaitForUrl("/login");
    }

    private void SelectWhenAvailable(string id, string value)
    {
        new WebDriverWait(Driver, TestSettings.Timeout).Until(browser =>
            browser.FindElements(By.CssSelector($"#{id} option[value='{value}']")).Any(option => option.Enabled));
        Select(By.Id(id), value);
    }
}
