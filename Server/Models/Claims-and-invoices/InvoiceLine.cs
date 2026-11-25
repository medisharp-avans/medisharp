namespace Server.Models
{
    /// <summary>One invoiced appointment within an invoice (table invoice_lines). An appointment is invoiced at most once.</summary>
    public class InvoiceLine
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public int AppointmentId { get; set; }
        public Appointment Appointment { get; set; } = null!;

        public decimal Amount { get; set; }
    }
}
