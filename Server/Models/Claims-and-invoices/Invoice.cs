using Server.Models.Enums;

namespace Server.Models
{
    /// <summary>An invoice to a patient (table invoices). The patient is derived via invoice lines → appointment.</summary>
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public InvoiceReason Reason { get; set; }
        public DateOnly InvoiceDate { get; set; }
        public DateOnly DueDate { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
        public DateOnly? PaidAt { get; set; }

        public ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
        public PaymentPlan? PaymentPlan { get; set; }
        public ICollection<PaymentReminder> PaymentReminders { get; set; } = new List<PaymentReminder>();
    }
}
