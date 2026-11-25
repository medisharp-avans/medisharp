namespace Server.Models.Enums
{
    /// <summary>Payment status of an invoice (lookup table invoice_statuses).</summary>
    public enum InvoiceStatus
    {
        /// <summary>Sent, not yet paid.</summary>
        Open,

        /// <summary>Fully paid.</summary>
        Paid,

        /// <summary>Handed over to a collection agency.</summary>
        SentToCollection,

        /// <summary>Will not be collected any more.</summary>
        WrittenOff
    }
}
