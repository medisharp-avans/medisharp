
using Server.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Gebruikersnaam is verplicht.")]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required] 
        [StringLength(100)] 
        public string SurName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(UserRole))]
        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsLockedOut { get; set; } = false;

        public Patient? PatientDetails { get; set; }

        public Specialist? SpecialistDetails { get; set; }

        public GeneralPractitioner? GeneralPractitionerDetails { get; set; }

        public HospitalEmployee? HospitalEmployeeDetails { get; set; }
    }
}
