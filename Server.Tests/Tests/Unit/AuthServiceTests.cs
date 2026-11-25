using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Server.Configuration;
using Server.Models;
using Server.Models.DTO.Requests;
using Server.Models.DTO.Responses;
using Server.Models.Enums;
using Server.Repositories.InterFaces;
using Server.Services;
using Server.Services.InterFaces;

namespace Server.Tests.Unit
{
    [TestClass]
    public class AuthServiceTests
    {
        private Mock<IUserRepository> _usersMock = null!;
        private Mock<IPasswordHasher<User>> _hasherMock = null!;
        private Mock<IEmailService> _emailMock = null!;
        private FixedTimeProvider _clock = null!;
        private string? _sentTo;
        private string? _sentBody;
        private JwtOptions _jwt = null!;
        private AuthService _sut = null!;
        private User _user = null!;
        private FakeRefreshTokenRepository _refreshTokens = null!;

        private static readonly DateTimeOffset Now = new(2027, 1, 12, 10, 0, 0, TimeSpan.Zero);

        /// <summary>In-memory refresh token store, so rotation and revocation can be checked end to end.</summary>
        private sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
        {
            public List<RefreshToken> Tokens { get; } = [];
            private int _nextId = 1;

            public Task<RefreshToken?> GetByHashAsync(string tokenHash) =>
                Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

            public Task<List<RefreshToken>> GetUnrevokedByUserAsync(int userId) =>
                Task.FromResult(Tokens.Where(t => t.UserId == userId && t.RevokedAt == null).ToList());

            public Task AddAsync(RefreshToken entity)
            {
                entity.Id = _nextId++;
                Tokens.Add(entity);
                return Task.CompletedTask;
            }

            public void Delete(RefreshToken entity) => Tokens.Remove(entity);
            public Task<RefreshToken?> GetByIdAsync(int id) => Task.FromResult(Tokens.FirstOrDefault(t => t.Id == id));
            public Task<List<RefreshToken>> GetAllAsync() => Task.FromResult(Tokens.ToList());
            public void Update(RefreshToken entity) { }
            public Task SaveChangesAsync() => Task.CompletedTask;
        }

        /// <summary>Clock that always returns <see cref="Now"/>, so expiry times are predictable.</summary>
        private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
        {
            public DateTimeOffset Now { get; set; } = now;
            public override DateTimeOffset GetUtcNow() => Now;
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
                ExpiresMinutes = 5,
                RefreshTokenMinutes = 30
            };
            _refreshTokens = new FakeRefreshTokenRepository();
            _emailMock = new Mock<IEmailService>();
            _emailMock
                .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((to, _, body) => { _sentTo = to; _sentBody = body; })
                .Returns(Task.CompletedTask);
            _clock = new FixedTimeProvider(Now);
            _sut = new AuthService(_usersMock.Object, _refreshTokens, _hasherMock.Object, Options.Create(_jwt), _clock,
                _emailMock.Object, new MemoryCache(new MemoryCacheOptions()));

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
            _usersMock.Setup(u => u.GetByIdAsync(5)).ReturnsAsync(_user);
            _hasherMock
                .Setup(h => h.VerifyHashedPassword(_user, "stored-hash", "Geheim123!"))
                .Returns(PasswordVerificationResult.Success);
        }

        private static LoginRequestDTO Request(string email = "jan@example.com", string password = "Geheim123!") =>
            new() { Email = email, Password = password };

        /// <summary>The 6 digit code from the last email that was sent.</summary>
        private string SentCode() => Regex.Match(_sentBody!, @"\b\d{6}\b").Value;

        private static string OtherCode(string code) => code == "000000" ? "111111" : "000000";

        /// <summary>Step 1 and step 2 with the emailed code.</summary>
        private async Task<LoginResponseDTO?> FullLoginAsync()
        {
            var challenge = await _sut.LoginAsync(Request());
            Assert.IsNotNull(challenge);
            return await _sut.VerifyLoginAsync(Verify(challenge.ChallengeId, SentCode()));
        }

        private static VerifyLoginRequestDTO Verify(Guid challengeId, string code) =>
            new() { ChallengeId = challengeId, Code = code };

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

        // ---------- Step 1: password -> email code (AC-PRI-04_1) ----------

        [TestMethod]
        public async Task LoginAsync_ValidCredentials_SendsSixDigitCodeToUsersEmail()
        {
            var result = await _sut.LoginAsync(Request());

            Assert.IsNotNull(result);
            Assert.AreEqual("jan@example.com", _sentTo);
            Assert.MatchesRegex(@"^\d{6}$", SentCode());
            _emailMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task LoginAsync_ValidCredentials_ReturnsChallengeWithoutLoggingIn()
        {
            var result = await _sut.LoginAsync(Request());

            Assert.IsNotNull(result);
            Assert.AreNotEqual(Guid.Empty, result.ChallengeId);
            Assert.AreEqual("j***@example.com", result.MaskedEmail);
            Assert.AreEqual(Now.UtcDateTime.Add(AuthService.CodeLifetime), result.ExpiresAt);
            Assert.IsNull(_user.LastLoginAt, "Not logged in before the code is checked");
        }

        // ---------- Wrong code (AC-PRI-04_2) ----------

        [TestMethod]
        public async Task VerifyLoginAsync_WrongCode_DeniesAccess()
        {
            var challenge = await _sut.LoginAsync(Request());
            Assert.IsNotNull(challenge);

            var result = await _sut.VerifyLoginAsync(Verify(challenge.ChallengeId, OtherCode(SentCode())));

            Assert.IsNull(result);
            Assert.IsNull(_user.LastLoginAt);
        }

        [TestMethod]
        public async Task VerifyLoginAsync_UnknownChallenge_DeniesAccess()
        {
            await _sut.LoginAsync(Request());

            Assert.IsNull(await _sut.VerifyLoginAsync(Verify(Guid.NewGuid(), SentCode())));
        }

        [TestMethod]
        public async Task VerifyLoginAsync_ExpiredCode_DeniesAccess()
        {
            var challenge = await _sut.LoginAsync(Request());
            Assert.IsNotNull(challenge);
            _clock.Now = Now.Add(AuthService.CodeLifetime);

            Assert.IsNull(await _sut.VerifyLoginAsync(Verify(challenge.ChallengeId, SentCode())));
        }

        [TestMethod]
        public async Task VerifyLoginAsync_CodeUsedTwice_SecondTimeDenied()
        {
            var challenge = await _sut.LoginAsync(Request());
            Assert.IsNotNull(challenge);

            Assert.IsNotNull(await _sut.VerifyLoginAsync(Verify(challenge.ChallengeId, SentCode())));
            Assert.IsNull(await _sut.VerifyLoginAsync(Verify(challenge.ChallengeId, SentCode())));
        }

        [TestMethod]
        public async Task VerifyLoginAsync_TooManyWrongAttempts_CorrectCodeDeniedAfterwards()
        {
            var challenge = await _sut.LoginAsync(Request());
            Assert.IsNotNull(challenge);
            var code = SentCode();

            for (var i = 0; i < AuthService.MaxCodeAttempts; i++)
                await _sut.VerifyLoginAsync(Verify(challenge.ChallengeId, OtherCode(code)));

            Assert.IsNull(await _sut.VerifyLoginAsync(Verify(challenge.ChallengeId, code)));
        }

        [TestMethod]
        public async Task VerifyLoginAsync_UserDeactivatedBetweenSteps_DeniesAccess()
        {
            var challenge = await _sut.LoginAsync(Request());
            Assert.IsNotNull(challenge);
            _user.IsActive = false;

            Assert.IsNull(await _sut.VerifyLoginAsync(Verify(challenge.ChallengeId, SentCode())));
        }

        // ---------- Step 2: correct code -> JWT ----------

        [TestMethod]
        public async Task VerifyLoginAsync_CorrectCode_ReturnsUserWithoutSensitiveFields()
        {
            var result = await FullLoginAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.User.Id);
            Assert.AreEqual("jan@example.com", result.User.Email);
            Assert.AreEqual("Jan de Vries", result.User.FullName);
            Assert.AreEqual("jan@example.com", result.User.Email);
            Assert.AreEqual(UserRole.Patient, result.User.Role);
        }

        [TestMethod]
        public async Task VerifyLoginAsync_CorrectCode_ExpiresAfterConfiguredMinutes()
        {
            var result = await FullLoginAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(Now.UtcDateTime.AddMinutes(5), result.ExpiresAt);
        }

        [TestMethod]
        public async Task VerifyLoginAsync_CorrectCode_ReturnsSignedTokenWithUserClaims()
        {
            var result = await FullLoginAsync();

            Assert.IsNotNull(result);
            var token = ValidateToken(result.Token);
            Assert.AreEqual("medisharp-test", token.Issuer);
            CollectionAssert.Contains(token.Audiences.ToList(), "medisharp-client");
            Assert.AreEqual("5", token.Subject);
            Assert.AreEqual("5", token.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.AreEqual("jan@example.com", token.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
            Assert.AreEqual(nameof(UserRole.Patient), token.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
            Assert.AreEqual(Now.UtcDateTime, token.ValidFrom);
            Assert.AreEqual(Now.UtcDateTime.AddMinutes(5), token.ValidTo);
        }

        [TestMethod]
        public async Task VerifyLoginAsync_TokenSignedWithOtherKey_FailsValidation()
        {
            var result = await FullLoginAsync();
            Assert.IsNotNull(result);

            _jwt.Key = "a-completely-different-key-of-32-chars!";

            Assert.ThrowsExactly<SecurityTokenSignatureKeyNotFoundException>(() => ValidateToken(result.Token));
        }

        [TestMethod]
        public async Task VerifyLoginAsync_TwoLogins_GiveDifferentTokenIds()
        {
            var first = await FullLoginAsync();
            var second = await FullLoginAsync();

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            Assert.AreNotEqual(ValidateToken(first.Token).Id, ValidateToken(second.Token).Id);
        }

        [TestMethod]
        public async Task VerifyLoginAsync_CorrectCode_StoresLastLoginAndSaves()
        {
            await FullLoginAsync();

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

        // ---------- Refresh token: stay logged in for 30 minutes without activity ----------

        private static RefreshTokenRequestDTO RefreshRequest(string token) => new() { RefreshToken = token };

        [TestMethod]
        public async Task VerifyLoginAsync_CorrectCode_ReturnsRefreshTokenValidFor30Minutes()
        {
            var result = await FullLoginAsync();

            Assert.IsNotNull(result);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.RefreshToken));
            Assert.AreEqual(Now.UtcDateTime.AddMinutes(30), result.RefreshTokenExpiresAt);
            var stored = _refreshTokens.Tokens.Single();
            Assert.AreEqual(5, stored.UserId);
            Assert.AreNotEqual(result.RefreshToken, stored.TokenHash, "Only the hash is stored");
        }

        [TestMethod]
        public async Task RefreshAsync_ValidToken_ReturnsNewTokensAndExtendsSession()
        {
            var login = await FullLoginAsync();
            Assert.IsNotNull(login);
            _clock.Now = Now.AddMinutes(20);

            var result = await _sut.RefreshAsync(RefreshRequest(login.RefreshToken));

            Assert.IsNotNull(result);
            Assert.AreNotEqual(login.RefreshToken, result.RefreshToken);
            Assert.AreEqual(Now.UtcDateTime.AddMinutes(25), result.ExpiresAt);
            Assert.AreEqual(Now.UtcDateTime.AddMinutes(50), result.RefreshTokenExpiresAt);
            Assert.AreEqual("5", ValidateToken(result.Token).Subject);
            Assert.AreEqual(5, result.User.Id);
        }

        [TestMethod]
        public async Task RefreshAsync_TokenUsedTwice_SecondTimeDeniedAndAllSessionsEnded()
        {
            var login = await FullLoginAsync();
            Assert.IsNotNull(login);
            var rotated = await _sut.RefreshAsync(RefreshRequest(login.RefreshToken));
            Assert.IsNotNull(rotated);

            Assert.IsNull(await _sut.RefreshAsync(RefreshRequest(login.RefreshToken)));

            // The new token was revoked as well, because the old one was reused (possible theft).
            Assert.IsNull(await _sut.RefreshAsync(RefreshRequest(rotated.RefreshToken)));
        }

        [TestMethod]
        public async Task RefreshAsync_After30MinutesWithoutActivity_Denied()
        {
            var login = await FullLoginAsync();
            Assert.IsNotNull(login);
            _clock.Now = Now.AddMinutes(30);

            Assert.IsNull(await _sut.RefreshAsync(RefreshRequest(login.RefreshToken)));
        }

        [TestMethod]
        public async Task RefreshAsync_UnknownToken_Denied()
        {
            await FullLoginAsync();

            Assert.IsNull(await _sut.RefreshAsync(RefreshRequest("not-a-real-token")));
        }

        [TestMethod]
        public async Task RefreshAsync_UserDeactivated_DeniedAndTokenRevoked()
        {
            var login = await FullLoginAsync();
            Assert.IsNotNull(login);
            _user.IsActive = false;

            Assert.IsNull(await _sut.RefreshAsync(RefreshRequest(login.RefreshToken)));
            Assert.IsNotNull(_refreshTokens.Tokens.Single().RevokedAt);
        }

        [TestMethod]
        public async Task LogoutAsync_RevokesRefreshToken()
        {
            var login = await FullLoginAsync();
            Assert.IsNotNull(login);

            await _sut.LogoutAsync(RefreshRequest(login.RefreshToken));

            Assert.IsNull(await _sut.RefreshAsync(RefreshRequest(login.RefreshToken)));
        }

        [TestMethod]
        public async Task LogoutAsync_UnknownToken_DoesNotThrow()
        {
            await _sut.LogoutAsync(RefreshRequest("not-a-real-token"));
        }

        [TestMethod]
        public async Task VerifyLoginAsync_NewLogin_RemovesExpiredTokensOfUser()
        {
            await FullLoginAsync();
            _clock.Now = Now.AddMinutes(31);

            await FullLoginAsync();

            Assert.AreEqual(1, _refreshTokens.Tokens.Count);
            Assert.AreEqual(Now.UtcDateTime.AddMinutes(61), _refreshTokens.Tokens.Single().ExpiresAt);
        }
    }
}
