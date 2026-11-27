
namespace Server.Models
{
    public class GeneralPractitioner
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public ICollection<Patient> Patients { get; set; }
            = new List<Patient>();

        public ICollection<Referral> Referrals { get; set; }
            = new List<Referral>();
    }
}
