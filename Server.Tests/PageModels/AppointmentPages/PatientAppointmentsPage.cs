using OpenQA.Selenium;

namespace Server.Tests.PageModels.AppointmentPages
{
    /// <summary>Appointments overview (/afspraken) in the patient portal.</summary>
    public class PatientAppointmentsPage(IWebDriver driver) : BasePage(driver)
    {
        public PatientAppointmentsPage Open()
        {
            Navigate("afspraken");
            return this;
        }

        /// <summary>True when the page offers any way to pick an appointment or a time slot.</summary>
        public bool HasPlanFunction =>
            Exists(TestId("plan-appointment")) || Exists(TestId("open-timeslots")) || Exists(TestIdStartsWith("slot-"));

        public IReadOnlyList<string> AppointmentRows =>
            Driver.FindElements(TestId("appointment-row")).Select(e => e.Text).ToList();
    }
}