
using Server.Models.Enums;

namespace Server.Models
{
    public class Specialist
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public Specialism Specialism { get; set; }

        public string? EncryptedIcalUrl { get; set; }

        public DateTime? LastSyncedUtc { get; set; }

        public ICollection<Appointment> Appointments { get; set; }
            = new List<Appointment>();
    }
}
