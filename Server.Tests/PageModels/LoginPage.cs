using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Server.Tests.TestDb;
using Server.Tests.Utils;

namespace Server.Tests.PageModels
{
    /// <summary>The login page: email and password, then the code from the email (PRI-03 and PRI-04).</summary>
    public class LoginPage(IWebDriver driver) : BasePage(driver)
    {
        // BaseInput puts the data-testid on its wrapper, so the input is found inside it
        private static readonly By EmailField = By.CssSelector("[data-testid='login-email'] input");
        private static readonly By PasswordField = By.CssSelector("[data-testid='login-password'] input");
        private static readonly By CodeField = By.CssSelector("[data-testid='login-code'] input");
        private static readonly By SubmitButton = TestId("login-submit");

        /// <summary>Logs in with both steps and waits until the app has left the login page.</summary>
        public void LoginAs(string email, string password = TestDbInitializer.Password)
        {
            Navigate("login");
            Type(EmailField, email, "e-mailadres");
            Type(PasswordField, password, "wachtwoord");

            var mailsBefore = TestMailbox.Snapshot();
            Click(SubmitButton, "inloggen");

            Driver.WaitForElement(CodeField);
            Type(CodeField, TestMailbox.WaitForLoginCode(email, mailsBefore), "verificatiecode");
            Click(SubmitButton, "bevestigen");

            new WebDriverWait(Driver, TestSettings.Timeout).Until(d => !new Uri(d.Url).AbsolutePath.StartsWith("/login"));
        }
    }
}
