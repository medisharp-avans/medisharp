using System.ComponentModel.DataAnnotations;

namespace Server.Models.DTO.Requests
{
    /// <summary>Login with an email address and a password.</summary>
    public class LoginRequestDTO
    {
        [Required(ErrorMessage = "E-mailadres is verplicht.")]
        [EmailAddress(ErrorMessage = "Ongeldig e-mailadres.")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wachtwoord is verplicht.")]
        [StringLength(128)]
        public string Password { get; set; } = string.Empty;
    }
}
