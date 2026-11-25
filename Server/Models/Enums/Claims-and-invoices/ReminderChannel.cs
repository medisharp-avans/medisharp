namespace Server.Models.Enums
{
    /// <summary>Channel through which a payment reminder was sent.</summary>
    public enum ReminderChannel
    {
        /// <summary>Sent by e-mail.</summary>
        Email,

        /// <summary>Done by phone.</summary>
        Phone
    }
}
