using OpenQA.Selenium;

namespace Server.Tests.PageModels.AppointmentPages
{
    /// <summary>Patients overview (/patienten) for staff.</summary>
    public class StaffPatientsPage(IWebDriver driver) : BasePage(driver)
    {
        private static readonly By SearchField = TestId("patient-search");

        public StaffPatientsPage Open()
        {
            Navigate("patienten");
            return this;
        }

        public StaffPatientsPage Search(string patientName)
        {
            Type(SearchField, patientName, "patient search");
            return this;
        }

        public void StartPlanning(string patientName) =>
            Click(By.XPath($"//*[@data-testid='patient-row'][contains(.,'{patientName}')]//*[@data-testid='plan-appointment']"),
                $"plan appointment for {patientName}");
    }
}