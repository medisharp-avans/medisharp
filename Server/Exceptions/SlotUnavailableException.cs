namespace Server.Exceptions
{
    /// <summary>Het tijdslot is bezet of de specialist is dan niet beschikbaar. De controller maakt hier een 409 van.</summary>
    public class SlotUnavailableException : Exception
    {
        public SlotUnavailableException()
        {
        }

        public SlotUnavailableException(string message)
            : base(message)
        {
        }

        public SlotUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}