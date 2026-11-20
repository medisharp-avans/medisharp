using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Server.Tests.Utils
{
    /// <summary>
    /// Explicit waits for Selenium. The client is a Vue app that renders in the browser,
    /// so always wait for what you need instead of using Thread.Sleep or an implicit wait.
    /// Every wait throws a WebDriverTimeoutException when it takes longer than TimeoutSeconds.
    /// </summary>
    public static class WebDriverExtensions
    {
        private static WebDriverWait Wait(IWebDriver driver) => new(driver, TestSettings.Timeout);

        /// <summary>Waits until the URL contains the given part, for example a path or a query parameter.</summary>
        public static void WaitForUrl(this IWebDriver driver, string urlPart) =>
            Wait(driver).Until(d => d.Url.Contains(urlPart));

        /// <summary>Waits until the element is on the page and visible.</summary>
        public static IWebElement WaitForElement(this IWebDriver driver, By by) =>
            Wait(driver).Until(d => d.FindElements(by).FirstOrDefault(e => e.Displayed))!;

        /// <summary>Waits until the element is gone from the page, for example because the view was replaced.</summary>
        public static void WaitForStaleness(this IWebDriver driver, IWebElement element) =>
            Wait(driver).Until(d =>
            {
                try
                {
                    _ = element.Enabled;
                    return false;
                }
                catch (WebDriverException e) when (e.IsElementGone())
                {
                    return true;
                }
            });

        /// <summary>
        /// True when the error means that the element is gone. Chrome reports that as a stale element,
        /// but while the old page is being replaced sometimes as "does not belong to the document".
        /// </summary>
        public static bool IsElementGone(this WebDriverException exception) =>
            exception is StaleElementReferenceException ||
            (exception is UnknownErrorException && exception.Message.Contains("does not belong to the document"));
    }
}
