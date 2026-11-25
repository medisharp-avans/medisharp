using System.ComponentModel.DataAnnotations;

namespace Server.Configuration
{
    /// <summary>
    /// JWT settings from the "Jwt" configuration section.
    /// </summary>
    public class JwtOptions
    {
        public const string SectionName = "JWT";

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        [Required]
        [MinLength(32)]
        public string Key { get; set; } = string.Empty;

        [Range(1, 1440)]
        public int ExpiresMinutes { get; set; } = 60;
    }
}
