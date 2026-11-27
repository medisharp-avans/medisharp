
namespace Server.Models
{
    public class Patient
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public string Bsn { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

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
