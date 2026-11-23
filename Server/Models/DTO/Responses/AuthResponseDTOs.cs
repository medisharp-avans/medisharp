using Server.Models.Enums;

namespace Server.Models.DTO.Responses
{
    /// <summary>The logged in user, without sensitive fields such as the password hash.</summary>
    public class AuthUserDTO
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
    }

    /// <summary>Result of a successful login.</summary>
    public class LoginResponseDTO
    {
        /// <summary>JWT to send as <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>When the token expires (UTC).</summary>
        public DateTime ExpiresAt { get; set; }

        public AuthUserDTO User { get; set; } = new();
    }
}
