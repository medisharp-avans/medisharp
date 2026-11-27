
using Server.Models.Enums;

namespace Server.Models
{
    public class User
    {
        public int Id { get; set; }

        public string SurName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsLockedOut { get; set; } = false;

        public Patient? PatientDetails { get; set; }

        public Specialist? SpecialistDetails { get; set; }

        public GeneralPractitioner? GeneralPractitionerDetails { get; set; }
    }
}
