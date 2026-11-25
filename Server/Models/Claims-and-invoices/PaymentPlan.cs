namespace Server.Models
{
    /// <summary>Installment arrangement for an invoice (table payment_plans). At most one per invoice.</summary>
    public class PaymentPlan
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public int NumberOfInstallments { get; set; }
        public decimal InstallmentAmount { get; set; }
        public DateOnly StartDate { get; set; }
        public string? Terms { get; set; }
    }
}
