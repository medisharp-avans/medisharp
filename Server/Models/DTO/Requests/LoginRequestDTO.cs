using System.ComponentModel.DataAnnotations;

namespace Server.Models.DTO.Requests
{
    /// <summary>Login with a username and a password.</summary>
    public class LoginRequestDTO
    {
        [Required(ErrorMessage = "Gebruikersnaam is verplicht.")]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Wachtwoord is verplicht.")]
        [StringLength(128)]
        public string Password { get; set; } = string.Empty;
    }
}
