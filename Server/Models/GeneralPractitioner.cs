using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class GeneralPractitioner
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public ICollection<Patient> Patients { get; set; }
            = new List<Patient>();

        public ICollection<Referral> Referrals { get; set; }
            = new List<Referral>();
    }
}