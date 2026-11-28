using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    /// <summary>
    /// A refresh token for one login session. Only the SHA-256 hash is stored, so a database leak
    /// does not give working tokens. Every refresh replaces the token (rotation).
    /// </summary>
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }

        /// <summary>SHA-256 of the token, hex encoded.</summary>
        [Required]
        [StringLength(64)]
        public string TokenHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        /// <summary>Set when the token was used (rotated) or the user logged out.</summary>
        public DateTime? RevokedAt { get; set; }

        public bool IsActive(DateTime now) => RevokedAt is null && now < ExpiresAt;
    }
}
