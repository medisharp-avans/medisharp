using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
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
        IPasswordHasher<User> passwordHasher,
        IOptions<JwtOptions> jwtOptions,
        TimeProvider timeProvider) : IAuthService
    {
        private readonly JwtOptions _jwt = jwtOptions.Value;

        public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO request)
        {
            var user = await users.GetByEmailAsync(request.Email.Trim());
            if (user is null || !user.IsActive)
                return null;

            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
                return null;

            // The hash was made with older settings; store it again with the current settings.
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
                user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

            var now = timeProvider.GetUtcNow().UtcDateTime;
            user.LastLoginAt = now;
            await users.SaveChangesAsync();

            var expiresAt = now.AddMinutes(_jwt.ExpiresMinutes);
            return new LoginResponseDTO
            {
                Token = CreateToken(user, now, expiresAt),
                ExpiresAt = expiresAt,
                User = ToDto(user)
            };
        }

        public async Task<AuthUserDTO?> GetCurrentUserAsync(int userId)
        {
            var user = await users.GetByIdAsync(userId);
            return user is { IsActive: true } ? ToDto(user) : null;
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

        private static AuthUserDTO ToDto(User user) => new()
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
    }
}
