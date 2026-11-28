using System.ComponentModel.DataAnnotations;

namespace Server.Models.DTO.Requests
{
    /// <summary>The refresh token from the last login or refresh.</summary>
    public class RefreshTokenRequestDTO
    {
        [Required(ErrorMessage = "Refresh token is verplicht.")]
        [StringLength(200)]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
