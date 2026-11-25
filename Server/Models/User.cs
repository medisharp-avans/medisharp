
using Server.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

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
        public UserRole Role { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsLockedOut { get; set; } = false;

        /// <summary>Moment of the last successful login (UTC).</summary>
        public DateTime? LastLoginAt { get; set; }

        public Patient? PatientDetails { get; set; }

        public Specialist? SpecialistDetails { get; set; }

        public GeneralPractitioner? GeneralPractitionerDetails { get; set; }
    }
}
