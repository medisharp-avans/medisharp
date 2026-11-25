using Server.Models.Enums;

namespace Server.Models
{
    /// <summary>A payment reminder for an unpaid invoice (table payment_reminders).</summary>
    public class PaymentReminder
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        /// <summary>1 for the first reminder, 2 for the second.</summary>
        public int SequenceNumber { get; set; }
        public DateTime SentAt { get; set; }
        public ReminderChannel Channel { get; set; }
    }
}
