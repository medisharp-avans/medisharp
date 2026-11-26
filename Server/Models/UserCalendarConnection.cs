using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class UserCalendarConnection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SpecialistId { get; set; }

        public Specialist Specialist { get; set; } = null!;

        public string EncryptedIcalUrl { get; set; } = default!; 
        public DateTime? LastSyncedUtc { get; set; }
    }
}
