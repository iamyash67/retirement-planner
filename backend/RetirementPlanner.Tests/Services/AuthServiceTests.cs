using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MySqlConnector;
using RetirementPlanner.Auth;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Services;
using RetirementPlanner.Services.Interfaces;
using RetirementPlanner.Tests.Auth;
using RetirementPlanner.Tests.TestSupport;

namespace RetirementPlanner.Tests.Services
{
    public class AuthServiceTests
    {
        private static readonly DateTimeOffset Now = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);
        private static readonly Guid Family = Guid.Parse("7b6f2c1e-0d4a-4e8b-9c3f-2a1b0c9d8e7f");
        private static readonly AuthenticatedUser User = new() { UserId = 7, Email = "jane@example.com", FirstName = "Jane" };

        private readonly Mock<IUnitOfWork> _unitOfWork = UnitOfWorkMock.Create<AuthResult>();
        private readonly Mock<IUserService> _userService = new();
        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IProfileRepository> _profileRepo = new();
        private readonly Mock<IRefreshTokenRepository> _tokens = new();
        private readonly List<RefreshToken> _created = [];

        public AuthServiceTests()
        {
            _userService.Setup(s => s.GetAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(User);
            _tokens.Setup(r => r.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
                .Callback<RefreshToken, CancellationToken>((t, _) => _created.Add(t))
                .ReturnsAsync(() => 100 + _created.Count);
            // LogoutAsync uses ExecuteInTransactionAsync<bool>.
            _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>(), It.IsAny<CancellationToken>()))
                .Returns((Func<Task<bool>> work, CancellationToken _) => work());
        }

        private AuthService CreateService() => new(
            _unitOfWork.Object, _userService.Object, _userRepo.Object, _profileRepo.Object, _tokens.Object,
            new PasswordHasher<User>(), new JwtAccessTokenService(JwtAccessTokenServiceTests.Options, new FixedTimeProvider(Now)),
            JwtAccessTokenServiceTests.Options, new FixedTimeProvider(Now), NullLogger<AuthService>.Instance);

        private void Stored(RefreshToken token) =>
            _tokens.Setup(r => r.GetByHashForUpdateAsync(token.TokenHash, It.IsAny<CancellationToken>())).ReturnsAsync(token);

        private static RefreshToken ActiveToken(string raw) => new()
        {
            Id = 5,
            UserId = 7,
            FamilyId = Family,
            TokenHash = RefreshTokenGenerator.Hash(raw),
            CreatedAt = Now.UtcDateTime.AddDays(-1),
            ExpiresAt = Now.UtcDateTime.AddDays(6)
        };

        [Fact]
        public async Task LoginAsync_WithValidCredentials_StartsANewFamilyAndStoresOnlyTheHash()
        {
            _userService.Setup(s => s.AuthenticateAsync("jane@example.com", "secret", It.IsAny<CancellationToken>())).ReturnsAsync(User);

            var result = await CreateService().LoginAsync("jane@example.com", "secret");

            Assert.Equal(AuthStatus.Success, result.Status);
            var session = result.Session!;
            var stored = Assert.Single(_created);
            Assert.Equal(7, stored.UserId);
            Assert.NotEqual(Guid.Empty, stored.FamilyId);
            Assert.Equal(RefreshTokenGenerator.Hash(session.RefreshToken), stored.TokenHash);
            Assert.NotEqual(session.RefreshToken, stored.TokenHash);
            Assert.Equal(Now.UtcDateTime, stored.CreatedAt);
            Assert.Equal(Now.UtcDateTime.AddDays(7), stored.ExpiresAt);
            Assert.Equal(Now.AddDays(7), session.RefreshTokenExpiresAt);
            Assert.Equal(Now.AddMinutes(15), session.AccessToken.ExpiresAt);
        }

        [Fact]
        public async Task LoginAsync_WithInvalidCredentials_IssuesNothing()
        {
            var result = await CreateService().LoginAsync("jane@example.com", "wrong");

            Assert.Equal(AuthStatus.InvalidCredentials, result.Status);
            Assert.Null(result.Session);
            Assert.Empty(_created);
        }

        [Fact]
        public async Task RefreshAsync_WithActiveToken_RotatesWithinTheSameFamily()
        {
            var stored = ActiveToken("old-token");
            Stored(stored);

            var result = await CreateService().RefreshAsync("old-token");

            Assert.Equal(AuthStatus.Success, result.Status);
            var replacement = Assert.Single(_created);
            Assert.Equal(Family, replacement.FamilyId);
            Assert.Equal(RefreshTokenGenerator.Hash(result.Session!.RefreshToken), replacement.TokenHash);
            Assert.NotEqual("old-token", result.Session.RefreshToken);
            // The presented token is revoked and points at its replacement (id 101 from the mock).
            _tokens.Verify(r => r.RevokeAsync(5, Now.UtcDateTime, 101, It.IsAny<CancellationToken>()), Times.Once);
            _tokens.Verify(r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_WithTokenRotatedMinutesAgo_RevokesTheWholeFamily()
        {
            var reused = ActiveToken("stolen-token");
            reused.RevokedAt = Now.UtcDateTime.AddMinutes(-5);
            reused.ReplacedByTokenId = 6;
            Stored(reused);

            var result = await CreateService().RefreshAsync("stolen-token");

            Assert.Equal(AuthStatus.RefreshTokenReused, result.Status);
            Assert.Null(result.Session);
            _tokens.Verify(r => r.RevokeFamilyAsync(Family, Now.UtcDateTime, It.IsAny<CancellationToken>()), Times.Once);
            Assert.Empty(_created);
        }

        [Fact]
        public async Task RefreshAsync_WithTokenRotated5SecondsAgo_IsRefused_WithoutRevokingTheFamily()
        {
            // Two tabs refreshed with the same cookie; this is the one that lost the race.
            var rotated = ActiveToken("raced-token");
            rotated.RevokedAt = Now.UtcDateTime.AddSeconds(-5);
            rotated.ReplacedByTokenId = 6;
            Stored(rotated);

            var result = await CreateService().RefreshAsync("raced-token");

            Assert.Equal(AuthStatus.InvalidRefreshToken, result.Status);
            Assert.Null(result.Session);
            Assert.Empty(_created);
            _tokens.Verify(r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_WithTokenRotated15SecondsAgo_IsReuse_AndRevokesTheFamily()
        {
            var rotated = ActiveToken("replayed-token");
            rotated.RevokedAt = Now.UtcDateTime.AddSeconds(-15);
            rotated.ReplacedByTokenId = 6;
            Stored(rotated);

            var result = await CreateService().RefreshAsync("replayed-token");

            Assert.Equal(AuthStatus.RefreshTokenReused, result.Status);
            _tokens.Verify(r => r.RevokeFamilyAsync(Family, Now.UtcDateTime, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RefreshAsync_WithTokenRotatedExactlyAtTheGraceLimit_IsStillRefusedWithoutRevoking()
        {
            var rotated = ActiveToken("edge-token");
            rotated.RevokedAt = Now.UtcDateTime.AddSeconds(-JwtAccessTokenServiceTests.Options.RefreshTokenReuseGraceSeconds);
            rotated.ReplacedByTokenId = 6;
            Stored(rotated);

            Assert.Equal(AuthStatus.InvalidRefreshToken, (await CreateService().RefreshAsync("edge-token")).Status);
            _tokens.Verify(r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_WithTokenRevokedByLogout5SecondsAgo_IsReuse_AndRevokesTheFamily()
        {
            // Logout and family revocation leave no replacement, so the grace period never applies to them.
            var loggedOut = ActiveToken("logged-out-token");
            loggedOut.RevokedAt = Now.UtcDateTime.AddSeconds(-5);
            loggedOut.ReplacedByTokenId = null;
            Stored(loggedOut);

            var result = await CreateService().RefreshAsync("logged-out-token");

            Assert.Equal(AuthStatus.RefreshTokenReused, result.Status);
            _tokens.Verify(r => r.RevokeFamilyAsync(Family, Now.UtcDateTime, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RefreshAsync_WithGracePeriodSetToZero_TreatsAnyReuseAsTheft()
        {
            var options = new JwtOptions
            {
                Issuer = JwtAccessTokenServiceTests.Options.Issuer,
                Audience = JwtAccessTokenServiceTests.Options.Audience,
                SigningKey = JwtAccessTokenServiceTests.Options.SigningKey,
                RefreshTokenReuseGraceSeconds = 0
            };
            var service = new AuthService(
                _unitOfWork.Object, _userService.Object, _userRepo.Object, _profileRepo.Object, _tokens.Object,
                new PasswordHasher<User>(), new JwtAccessTokenService(options, new FixedTimeProvider(Now)),
                options, new FixedTimeProvider(Now), NullLogger<AuthService>.Instance);
            var rotated = ActiveToken("raced-token");
            rotated.RevokedAt = Now.UtcDateTime.AddSeconds(-1);
            rotated.ReplacedByTokenId = 6;
            Stored(rotated);

            Assert.Equal(AuthStatus.RefreshTokenReused, (await service.RefreshAsync("raced-token")).Status);
        }

        [Fact]
        public async Task RefreshAsync_WithExpiredToken_Fails_WithoutRevokingTheFamily()
        {
            var expired = ActiveToken("expired-token");
            expired.ExpiresAt = Now.UtcDateTime;
            Stored(expired);

            var result = await CreateService().RefreshAsync("expired-token");

            Assert.Equal(AuthStatus.InvalidRefreshToken, result.Status);
            Assert.Empty(_created);
            _tokens.Verify(r => r.RevokeFamilyAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("unknown-token")]
        public async Task RefreshAsync_WithMissingOrUnknownToken_Fails(string? token)
        {
            var result = await CreateService().RefreshAsync(token);

            Assert.Equal(AuthStatus.InvalidRefreshToken, result.Status);
            Assert.Empty(_created);
        }

        [Fact]
        public async Task RefreshAsync_WhenUserNoLongerExists_Fails()
        {
            var stored = ActiveToken("orphan");
            stored.UserId = 8;
            Stored(stored);

            Assert.Equal(AuthStatus.InvalidRefreshToken, (await CreateService().RefreshAsync("orphan")).Status);
        }

        [Fact]
        public async Task LogoutAsync_RevokesTheFamily()
        {
            Stored(ActiveToken("current"));

            await CreateService().LogoutAsync("current");

            _tokens.Verify(r => r.RevokeFamilyAsync(Family, Now.UtcDateTime, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LogoutAsync_WithoutToken_DoesNothing()
        {
            await CreateService().LogoutAsync(null);

            _tokens.Verify(r => r.GetByHashForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WithNewEmail_CreatesUserProfileAndSession()
        {
            _userRepo.Setup(r => r.CreateAsync("jane@example.com", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(7);
            var command = new RegisterCommand("jane@example.com", "correct-horse", "Jane", "Doe", new DateOnly(1990, 6, 15), null);

            var result = await CreateService().RegisterAsync(command);

            Assert.Equal(AuthStatus.Success, result.Status);
            Assert.Same(User, result.Session!.User);
            _userRepo.Verify(r => r.CreateAsync("jane@example.com",
                It.Is<string>(hash => hash != "correct-horse" && hash.Length > 20), It.IsAny<CancellationToken>()), Times.Once);
            _profileRepo.Verify(r => r.CreateAsync(
                It.Is<UserProfile>(p => p.UserId == 7 && p.FirstName == "Jane" && p.DateOfBirth == new DateOnly(1990, 6, 15)),
                It.IsAny<CancellationToken>()), Times.Once);
            Assert.Single(_created);
        }

        [Fact]
        public async Task RegisterAsync_WithTakenEmail_ReturnsEmailTaken()
        {
            _userRepo.Setup(r => r.GetByEmailAsync("jane@example.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User { Id = 7, Email = "jane@example.com" });

            var result = await CreateService().RegisterAsync(
                new RegisterCommand("jane@example.com", "correct-horse", "Jane", "Doe", new DateOnly(1990, 6, 15), null));

            Assert.Equal(AuthStatus.EmailTaken, result.Status);
            _userRepo.Verify(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_WhenConcurrentRegistrationWinsTheUniqueKey_ReturnsEmailTaken()
        {
            _userRepo.Setup(r => r.CreateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(MySqlExceptionFactory.Create(MySqlErrorCode.DuplicateKeyEntry));

            var result = await CreateService().RegisterAsync(
                new RegisterCommand("jane@example.com", "correct-horse", "Jane", "Doe", new DateOnly(1990, 6, 15), null));

            Assert.Equal(AuthStatus.EmailTaken, result.Status);
        }
    }
}
