using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Server.Configuration;
using Server.Models;
using Server.Models.DTO.Requests;
using Server.Models.Enums;
using Server.Repositories.InterFaces;
using Server.Services;

namespace Server.Tests.Unit
{
    [TestClass]
    public class AuthServiceTests
    {
        private Mock<IUserRepository> _usersMock = null!;
        private Mock<IPasswordHasher<User>> _hasherMock = null!;
        private JwtOptions _jwt = null!;
        private AuthService _sut = null!;
        private User _user = null!;

        private static readonly DateTimeOffset Now = new(2027, 1, 12, 10, 0, 0, TimeSpan.Zero);

        /// <summary>Clock that always returns <see cref="Now"/>, so expiry times are predictable.</summary>
        private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => now;
        }

        [TestInitialize]
        public void Setup()
        {
            _usersMock = new Mock<IUserRepository>();
            _hasherMock = new Mock<IPasswordHasher<User>>();
            _jwt = new JwtOptions
            {
                Issuer = "medisharp-test",
                Audience = "medisharp-client",
                Key = "test-signing-key-that-is-at-least-32-chars",
                ExpiresMinutes = 30
            };
            _sut = new AuthService(_usersMock.Object, _hasherMock.Object, Options.Create(_jwt), new FixedTimeProvider(Now));

            _user = new User
            {
                Id = 5,
                FullName = "Jan de Vries",
                Email = "jan@example.com",
                PasswordHash = "stored-hash",
                Role = UserRole.Patient,
                IsActive = true
            };

            // Default success path; individual tests override what they need
            _usersMock.Setup(u => u.GetByEmailAsync("jan@example.com")).ReturnsAsync(_user);
            _hasherMock
                .Setup(h => h.VerifyHashedPassword(_user, "stored-hash", "Geheim123!"))
                .Returns(PasswordVerificationResult.Success);
        }

        private static LoginRequestDTO Request(string email = "jan@example.com", string password = "Geheim123!") =>
            new() { Email = email, Password = password };

        private JwtSecurityToken ValidateToken(string token)
        {
            var parameters = new TokenValidationParameters
            {
                ValidIssuer = _jwt.Issuer,
                ValidAudience = _jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
                // The token was issued at a fixed time in the future; only check signature/issuer/audience here
                ValidateLifetime = false
            };
            new JwtSecurityTokenHandler().ValidateToken(token, parameters, out var validated);
            return (JwtSecurityToken)validated;
        }

        [TestMethod]
        public async Task LoginAsync_ValidCredentials_ReturnsUserWithoutSensitiveFields()
        {
            var result = await _sut.LoginAsync(Request());

            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.User.Id);
            Assert.AreEqual("jan@example.com", result.User.Email);
            Assert.AreEqual("Jan de Vries", result.User.FullName);
            Assert.AreEqual("jan@example.com", result.User.Email);
            Assert.AreEqual(UserRole.Patient, result.User.Role);
        }

        [TestMethod]
        public async Task LoginAsync_ValidCredentials_ExpiresAfterConfiguredMinutes()
        {
            var result = await _sut.LoginAsync(Request());

            Assert.IsNotNull(result);
            Assert.AreEqual(Now.UtcDateTime.AddMinutes(30), result.ExpiresAt);
        }

        [TestMethod]
        public async Task LoginAsync_ValidCredentials_ReturnsSignedTokenWithUserClaims()
        {
            var result = await _sut.LoginAsync(Request());

            Assert.IsNotNull(result);
            var token = ValidateToken(result.Token);
            Assert.AreEqual("medisharp-test", token.Issuer);
            CollectionAssert.Contains(token.Audiences.ToList(), "medisharp-client");
            Assert.AreEqual("5", token.Subject);
            Assert.AreEqual("5", token.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.AreEqual("jan@example.com", token.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
            Assert.AreEqual(nameof(UserRole.Patient), token.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
            Assert.AreEqual(Now.UtcDateTime, token.ValidFrom);
            Assert.AreEqual(Now.UtcDateTime.AddMinutes(30), token.ValidTo);
        }

        [TestMethod]
        public async Task LoginAsync_TokenSignedWithOtherKey_FailsValidation()
        {
            var result = await _sut.LoginAsync(Request());
            Assert.IsNotNull(result);

            _jwt.Key = "a-completely-different-key-of-32-chars!";

            Assert.ThrowsExactly<SecurityTokenSignatureKeyNotFoundException>(() => ValidateToken(result.Token));
        }

        [TestMethod]
        public async Task LoginAsync_TwoLogins_GiveDifferentTokenIds()
        {
            var first = await _sut.LoginAsync(Request());
            var second = await _sut.LoginAsync(Request());

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            Assert.AreNotEqual(ValidateToken(first.Token).Id, ValidateToken(second.Token).Id);
        }

        [TestMethod]
        public async Task LoginAsync_ValidCredentials_StoresLastLoginAndSaves()
        {
            await _sut.LoginAsync(Request());

            Assert.AreEqual(Now.UtcDateTime, _user.LastLoginAt);
            _usersMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_EmailWithSurroundingWhitespace_IsTrimmed()
        {
            var result = await _sut.LoginAsync(Request(email: "  jan@example.com  "));

            Assert.IsNotNull(result);
            _usersMock.Verify(u => u.GetByEmailAsync("jan@example.com"), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_UnknownUser_ReturnsNullWithoutCheckingPassword()
        {
            var result = await _sut.LoginAsync(Request(email: "onbekend@example.com"));

            Assert.IsNull(result);
            _hasherMock.Verify(h => h.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _usersMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [TestMethod]
        public async Task LoginAsync_WrongPassword_ReturnsNullAndSavesNothing()
        {
            _hasherMock
                .Setup(h => h.VerifyHashedPassword(_user, "stored-hash", "fout"))
                .Returns(PasswordVerificationResult.Failed);

            var result = await _sut.LoginAsync(Request(password: "fout"));

            Assert.IsNull(result);
            Assert.IsNull(_user.LastLoginAt);
            _usersMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [TestMethod]
        public async Task LoginAsync_InactiveUser_ReturnsNullEvenWithCorrectPassword()
        {
            _user.IsActive = false;

            var result = await _sut.LoginAsync(Request());

            Assert.IsNull(result);
            _usersMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [TestMethod]
        public async Task LoginAsync_RehashNeeded_StoresNewHashAndSucceeds()
        {
            _hasherMock
                .Setup(h => h.VerifyHashedPassword(_user, "stored-hash", "Geheim123!"))
                .Returns(PasswordVerificationResult.SuccessRehashNeeded);
            _hasherMock.Setup(h => h.HashPassword(_user, "Geheim123!")).Returns("new-hash");

            var result = await _sut.LoginAsync(Request());

            Assert.IsNotNull(result);
            Assert.AreEqual("new-hash", _user.PasswordHash);
            _usersMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_NoRehashNeeded_KeepsExistingHash()
        {
            await _sut.LoginAsync(Request());

            Assert.AreEqual("stored-hash", _user.PasswordHash);
            _hasherMock.Verify(h => h.HashPassword(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task GetCurrentUserAsync_ActiveUser_ReturnsDto()
        {
            _usersMock.Setup(u => u.GetByIdAsync(5)).ReturnsAsync(_user);

            var result = await _sut.GetCurrentUserAsync(5);

            Assert.IsNotNull(result);
            Assert.AreEqual("jan@example.com", result.Email);
        }

        [TestMethod]
        public async Task GetCurrentUserAsync_InactiveUser_ReturnsNull()
        {
            _user.IsActive = false;
            _usersMock.Setup(u => u.GetByIdAsync(5)).ReturnsAsync(_user);

            Assert.IsNull(await _sut.GetCurrentUserAsync(5));
        }

        [TestMethod]
        public async Task GetCurrentUserAsync_UnknownUser_ReturnsNull()
        {
            Assert.IsNull(await _sut.GetCurrentUserAsync(999));
        }
    }
}
