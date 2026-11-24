using OpenQA.Selenium;

namespace Server.Tests.PageModels
{
    /// <summary>Side menu of the portal (ids from the existing layout).</summary>
    public class PortalNavigation(IWebDriver driver) : BasePage(driver)
    {
        public void GoToHome() => Click(By.Id("HomeRoute-link"), "menu Home");
        public void GoToAppointments() => Click(By.Id("AppointmentsRoute-link"), "menu Afspraken");
        public void GoToNewAppointment() => Click(By.Id("NewAppointmentRoute-link"), "menu Afspraak aanmaken");
        public void GoToMedicalFile() => Click(By.Id("MedicalFileRoute-link"), "menu Medisch Dossier");
        public void GoToReferrals() => Click(By.Id("ReferralsRoute-link"), "menu Verwijsbrieven");
        public void GoToPatients() => Click(By.Id("PatientsRoute-link"), "menu Patiënten");
        public void GoToResults() => Click(By.Id("MedicalResultsRoute-link"), "menu Onderzoeksuitslagen");
    }
}
