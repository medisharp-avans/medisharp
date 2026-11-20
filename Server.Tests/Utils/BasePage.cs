using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Server.Tests.Utils
{
    internal abstract class BasePage
    {
        protected readonly IWebDriver Driver;
        protected readonly string BaseUrl;

        protected BasePage(IWebDriver driver, string baseUrl)
        {
            Driver = driver;
            BaseUrl = baseUrl;
        }

        public void Navigate(string path = "") =>
            Driver.Navigate().GoToUrl($"{BaseUrl}/{path.TrimStart('/')}");

        public void WaitForUrl(string path, int seconds = 4) =>
            new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds))
                .Until(d => d.Url.StartsWith($"{BaseUrl}/{path.TrimStart('/')}"));

        public IWebElement WaitForElement(By by, int seconds = 4) =>
            new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds))
                .Until(d => d.FindElement(by));

        public void DisableBrowserValidation() =>
            ((IJavaScriptExecutor)Driver).ExecuteScript(
                "document.querySelector('form').noValidate = true;");

        public void Logout() =>
            WaitForElement(By.Name("btn_logout")).Click();

        public void WaitUntilStale(IWebElement element, int seconds = 2)
        {
            try
            {
                new WebDriverWait(Driver, TimeSpan.FromSeconds(seconds)).Until(d =>
                {
                    try { _ = element.Enabled; return false; }
                    catch (StaleElementReferenceException) { return true; }
                });
            }
            catch (WebDriverTimeoutException) { }
        }

        public int CountEditOrDeleteControls() =>
            Convert.ToInt32(((IJavaScriptExecutor)Driver).ExecuteScript(@"
                const re = /\\b(delete|remove|edit|verwijder|bewerk|wijzig)/i;
                const main = document.querySelector('main');
                return Array.from(main.querySelectorAll('button,a,input,textarea,select,[onclick]'))
                    .filter(e => re.test(e.outerHTML)).length;"));

        public int CountFormsInContent() =>
            Convert.ToInt32(((IJavaScriptExecutor)Driver).ExecuteScript(
                "return document.querySelector('main').querySelectorAll('form,input,textarea,select').length;"));

        public int SendDeleteRequest(string fileName)
        {
            Driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(5);
            var status = ((IJavaScriptExecutor)Driver).ExecuteAsyncScript(@"
                const cb = arguments[arguments.length - 1];
                fetch('/LogFiles?handler=View&fileName=' + encodeURIComponent(arguments[0]), { method: 'DELETE' })
                    .then(r => cb(r.status)).catch(() => cb(-1));", fileName);
            return Convert.ToInt32(status);
        }

        public int SendRequest(string method, string url, bool withToken = false)
        {
            TestLog.Step($"Stuur een asychrone request naar={url} met als method={method} en token={withToken}");
            Driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(5);
            var status = ((IJavaScriptExecutor)Driver).ExecuteAsyncScript(@"
                const cb = arguments[arguments.length - 1];
                const opts = { method: arguments[0] };
                if (arguments[2]) {
                    const token = document.querySelector('input[name=__RequestVerificationToken]').value;
                    opts.headers = { 'Content-Type': 'application/x-www-form-urlencoded' };
                    opts.body = new URLSearchParams({ __RequestVerificationToken: token });
                }
                fetch(arguments[1], opts).then(r => cb(r.status)).catch(() => cb(-1));", method, url, withToken);
            return Convert.ToInt32(status);
        }
    }
}
