using System.Text.Json.Serialization;
using Server.Models.Enums;

namespace Server.Models.DTO.Responses
{
    /// <summary>The logged in user, without sensitive fields such as the password hash.</summary>
    public class AuthUserDTO
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public UserRole Role { get; set; }
    }

    /// <summary>Result of a correct email address and password: a code was sent by email.</summary>
    public class LoginChallengeDTO
    {
        /// <summary>Send this back with the code to <c>POST /api/login/verify</c>.</summary>
        public Guid ChallengeId { get; set; }

        /// <summary>When the code expires (UTC).</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Partly hidden email address, e.g. <c>j***@example.com</c>.</summary>
        public string MaskedEmail { get; set; } = string.Empty;
    }

    /// <summary>Result of a successful login.</summary>
    public class LoginResponseDTO
    {
        /// <summary>JWT to send as <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>When the token expires (UTC).</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>Send this to <c>POST /api/login/refresh</c> for a new JWT. Can be used once.</summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>When the refresh token expires (UTC): the end of the session without activity.</summary>
        public DateTime RefreshTokenExpiresAt { get; set; }

        public AuthUserDTO User { get; set; } = new();
    }
}
