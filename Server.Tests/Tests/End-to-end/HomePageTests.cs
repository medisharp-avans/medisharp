using OpenQA.Selenium;
using Server.Tests.PageModels;
using Server.Tests.Utils;

namespace Server.Tests.EndToEnd
{
    /// <summary>Smoke test: checks that the test infrastructure (database, API, client and browser) works.</summary>
    [TestClass]
    public class HomePageTests : EndToEndTestBase
    {
        [TestMethod]
        public void HomePage_Opens_ShowsMainNavigation()
        {
            // Arrange
            var homePage = new HomePage(Driver);

            // Act
            homePage.Navigate();

            // Assert
            Assert.IsTrue(Driver.WaitForElement(By.Id("main-navigation")).Displayed, "The main navigation is not shown.");
            Assert.AreEqual("MediSharp", Driver.Title);
        }

        private sealed class HomePage(IWebDriver driver) : BasePage(driver);
    }
}
