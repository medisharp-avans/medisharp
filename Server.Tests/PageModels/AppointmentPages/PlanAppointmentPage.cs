using OpenQA.Selenium;
using Server.Tests.Utils;

namespace Server.Tests.PageModels.AppointmentPages
{
    /// <summary>Available time slots and the confirmation of an appointment.</summary>
    public class PlanAppointmentPage(IWebDriver driver) : BasePage(driver)
    {
        private static readonly By OpenSlots = TestId("open-timeslots");
        private static readonly By SlotList = TestId("timeslot-list");
        private static readonly By ConfirmButton = TestId("confirm-appointment");
        private static readonly By MessageBox = TestId("appointment-message");

        private static string SlotName(string specialist, DateTime start) => $"slot-{specialist}-{start:yyyy-MM-dd HH:mm}";

        public PlanAppointmentPage OpenTimeSlots()
        {
            Click(OpenSlots, "open time slots");
            Driver.WaitForElement(SlotList);
            return this;
        }

        public bool HasPlanFunction => Exists(OpenSlots) || Exists(TestId("plan-appointment"));
        public bool HasSlot(string specialist, DateTime start) => Exists(TestId(SlotName(specialist, start)));
        public bool HasAnySlotOf(string specialist) => Exists(TestIdStartsWith($"slot-{specialist}-"));

        public void SelectSlot(string specialist, DateTime start) =>
            Click(TestId(SlotName(specialist, start)), $"slot {specialist} {start:dd-MM-yyyy HH:mm}");

        public void Confirm() => Click(ConfirmButton, "confirm appointment");

        public bool HasMessage => Exists(MessageBox);
        public string Message => Driver.WaitForElement(MessageBox).Text.Trim();

        public PlanAppointmentPage WaitForMessage()
        {
            _ = Message;
            return this;
        }
    }
}