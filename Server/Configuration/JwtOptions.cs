using System.ComponentModel.DataAnnotations;

namespace Server.Configuration
{
    /// <summary>
    /// JWT settings from the "Jwt" configuration section.
    /// In production set the key via the <c>Jwt__Key</c> environment variable, never in appsettings.json.
    /// </summary>
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        /// <summary>Signing key for HMAC-SHA256; at least 32 characters (256 bits).</summary>
        [Required]
        [MinLength(32)]
        public string Key { get; set; } = string.Empty;

        [Range(1, 1440)]
        public int ExpiresMinutes { get; set; } = 60;
    }
}
