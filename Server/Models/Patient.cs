
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User User { get; set; } = null!;

        [Required]
        [StringLength(9, MinimumLength = 9)]
        [RegularExpression(@"^\d{9}$")]
        public string Bsn { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string City { get; set; } = string.Empty;

        public int? GeneralPractitionerId { get; set; }

        public GeneralPractitioner? GeneralPractitioner { get; set; }

        public bool HasInsurance { get; set; } = true;

        public ICollection<Referral> Referrals { get; set; }
            = new List<Referral>();

        public ICollection<Appointment> Appointments { get; set; }
            = new List<Appointment>();
    }
}
