namespace Server.Models.Enums
{
    /// <summary>Why the patient receives an invoice.</summary>
    public enum InvoiceReason
    {
        /// <summary>The patient has no insurer.</summary>
        NoInsurer,

        /// <summary>The appointment was cancelled too late.</summary>
        LateCancellation,

        /// <summary>The insurer did not (fully) reimburse the care.</summary>
        NotReimbursed
    }
}
