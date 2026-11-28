
namespace Server.Models
{
    public class ExternalCalendarEvent
    {
        public int Id { get; set; }

        public int SpecialistId { get; set; }

        public Specialist Specialist { get; set; } = null!;

        public string ExternalUid { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public DateTime StartUtc { get; set; }

        public DateTime EndUtc { get; set; }

        public bool IsAllDay { get; set; }
    }
}
