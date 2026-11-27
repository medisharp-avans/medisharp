using System.Buffers.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server.Configuration;
using Server.Models;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Repositories.InterFaces;
using Server.Services.InterFaces;

namespace Server.Services
{
    public class AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher<User> passwordHasher,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider,
        IEmailService emailService,
        IMemoryCache cache) : IAuthService
    {
        public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);
        public const int MaxCodeAttempts = 5;

        private readonly JwtOptions _jwt = jwtOptions.Value;

        /// <summary>A login waiting for its email code. Only the hash of the code is kept.</summary>
        private sealed class PendingLogin(int userId, byte[] codeHash, DateTime expiresAt)
        {
            public int UserId { get; } = userId;
            public byte[] CodeHash { get; } = codeHash;
            public DateTime ExpiresAt { get; } = expiresAt;
            public int Attempts;
        }

        private static string CacheKey(Guid challengeId) => $"login-2fa:{challengeId}";

        public async Task<LoginChallengeDTO?> LoginAsync(LoginRequestDTO request)
        {
            var user = await users.GetByEmailAsync(request.Email.Trim());
            if (user is null || !user.IsActive)
                return null;

            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
                return null;

            // The hash was made with older settings; store it again with the current settings.
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
                await users.SaveChangesAsync();
            }

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var challengeId = Guid.NewGuid();
            var expiresAt = timeProvider.GetUtcNow().UtcDateTime.Add(CodeLifetime);
            cache.Set(CacheKey(challengeId), new PendingLogin(user.Id, Hash(code), expiresAt), CodeLifetime);

            await emailService.SendAsync(
                user.Email,
                "Uw MediSharp verificatiecode",
                $"Beste {user.FullName},\n\nUw verificatiecode is: {code}\n\n" +
                $"De code is {CodeLifetime.TotalMinutes} minuten geldig. Heeft u niet geprobeerd in te loggen? " +
                "Wijzig dan uw wachtwoord.\n\nMediSharp");

            return new LoginChallengeDTO
            {
                ChallengeId = challengeId,
                ExpiresAt = expiresAt,
                MaskedEmail = MaskEmail(user.Email)
            };
        }

        public async Task<LoginResponseDTO?> VerifyLoginAsync(VerifyLoginRequestDTO request)
        {
            var key = CacheKey(request.ChallengeId);
            if (!cache.TryGetValue(key, out PendingLogin? pending) || pending is null)
                return null;

            var now = timeProvider.GetUtcNow().UtcDateTime;
            if (now >= pending.ExpiresAt || Interlocked.Increment(ref pending.Attempts) > MaxCodeAttempts)
            {
                cache.Remove(key);
                return null;
            }

            if (!CryptographicOperations.FixedTimeEquals(pending.CodeHash, Hash(request.Code.Trim())))
                return null;

            // A code can be used once.
            cache.Remove(key);

            var user = await users.GetByIdAsync(pending.UserId);
            if (user is null || !user.IsActive)
                return null;

            user.LastLoginAt = now;
            await users.SaveChangesAsync();

            return await CreateSessionAsync(user, now);
        }

        public async Task<LoginResponseDTO?> RefreshAsync(RefreshTokenRequestDTO request)
        {
            var stored = await refreshTokens.GetByHashAsync(HashHex(request.RefreshToken));
            if (stored is null)
                return null;

            var now = timeProvider.GetUtcNow().UtcDateTime;

            // A token that was already used is presented again: it has probably been stolen.
            // End every session of this user, so the thief cannot continue either.
            if (stored.RevokedAt is not null)
            {
                await RevokeAllAsync(stored.UserId, now);
                return null;
            }

            if (!stored.IsActive(now))
                return null;

            // Rotation: the token can be used once.
            stored.RevokedAt = now;

            var user = await users.GetByIdAsync(stored.UserId);
            if (user is null || !user.IsActive)
            {
                await refreshTokens.SaveChangesAsync();
                return null;
            }

            return await CreateSessionAsync(user, now);
        }

        public async Task LogoutAsync(RefreshTokenRequestDTO request)
        {
            var stored = await refreshTokens.GetByHashAsync(HashHex(request.RefreshToken));
            if (stored is null || stored.RevokedAt is not null)
                return;

            stored.RevokedAt = timeProvider.GetUtcNow().UtcDateTime;
            await refreshTokens.SaveChangesAsync();
        }

        public async Task<AuthUserDTO?> GetCurrentUserAsync(int userId)
        {
            var user = await users.GetByIdAsync(userId);
            return user is { IsActive: true } ? ToDto(user) : null;
        }

        /// <summary>Creates a new JWT and a new refresh token, and stores the hash of the refresh token.</summary>
        private async Task<LoginResponseDTO> CreateSessionAsync(User user, DateTime now)
        {
            // Tidy up: expired tokens of this user are no longer needed.
            foreach (var old in await refreshTokens.GetUnrevokedByUserAsync(user.Id))
            {
                if (old.ExpiresAt <= now)
                    refreshTokens.Delete(old);
            }

            var refreshToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
            var refreshExpiresAt = now.AddMinutes(_jwt.RefreshTokenMinutes);
            await refreshTokens.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = HashHex(refreshToken),
                CreatedAt = now,
                ExpiresAt = refreshExpiresAt
            });
            await refreshTokens.SaveChangesAsync();

            var expiresAt = now.AddMinutes(_jwt.ExpiresMinutes);
            return new LoginResponseDTO
            {
                Token = CreateToken(user, now, expiresAt),
                ExpiresAt = expiresAt,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = refreshExpiresAt,
                User = ToDto(user)
            };
        }

        private async Task RevokeAllAsync(int userId, DateTime now)
        {
            foreach (var token in await refreshTokens.GetUnrevokedByUserAsync(userId))
                token.RevokedAt = now;
            await refreshTokens.SaveChangesAsync();
        }

        private string CreateToken(User user, DateTime now, DateTime expiresAt)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Token));
            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                notBefore: now,
                expires: expiresAt,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static byte[] Hash(string code) => SHA256.HashData(Encoding.UTF8.GetBytes(code));

        private static string HashHex(string token) => Convert.ToHexString(Hash(token));

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            return at <= 0 ? "***" : $"{email[0]}***{email[at..]}";
        }

        private static AuthUserDTO ToDto(User user) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
    }
}
