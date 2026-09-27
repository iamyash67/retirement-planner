using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using RetirementPlanner.Auth;
using RetirementPlanner.Data;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories;
using RetirementPlanner.Services;
using RetirementPlanner.Tests.Auth;
using RetirementPlanner.Tests.TestSupport;

namespace RetirementPlanner.Tests.Integration
{
    /// <summary>Refresh-token rotation and reuse detection against the real RefreshTokens table.</summary>
    [Collection(MySqlCollection.Name)]
    public class RefreshTokenFlowTests(MySqlFixture db)
    {
        private const string Password = "correct-horse";

        // Every service in a test shares this clock, so a test can step past the reuse grace period.
        private readonly ManualTimeProvider _clock = new(DateTimeOffset.UtcNow);

        [Fact]
        public async Task Refresh_RotatesTheToken_AndRecordsTheReplacement()
        {
            var email = await RegisterAsync();
            var login = await WithAuthServiceAsync(s => s.LoginAsync(email, Password));

            var refreshed = await WithAuthServiceAsync(s => s.RefreshAsync(login.Session!.RefreshToken));

            Assert.Equal(AuthStatus.Success, refreshed.Status);
            Assert.NotEqual(login.Session!.RefreshToken, refreshed.Session!.RefreshToken);

            var oldHash = RefreshTokenGenerator.Hash(login.Session.RefreshToken);
            var newHash = RefreshTokenGenerator.Hash(refreshed.Session.RefreshToken);
            Assert.Equal(1, await db.ScalarAsync<long>(
                """
                SELECT COUNT(*) FROM RefreshTokens old
                JOIN RefreshTokens replacement ON replacement.Id = old.ReplacedByTokenId
                WHERE old.TokenHash = @oldHash AND old.RevokedAt IS NOT NULL
                  AND replacement.TokenHash = @newHash AND replacement.RevokedAt IS NULL
                  AND replacement.FamilyId = old.FamilyId
                """, new { oldHash, newHash }));

            // The raw token is never stored.
            Assert.Equal(0, await db.ScalarAsync<long>(
                "SELECT COUNT(*) FROM RefreshTokens WHERE TokenHash = @raw", new { raw = refreshed.Session.RefreshToken }));
        }

        [Fact]
        public async Task ReusingARotatedToken_RevokesTheWholeFamily_ButNotOtherSessions()
        {
            var email = await RegisterAsync();
            var first = (await WithAuthServiceAsync(s => s.LoginAsync(email, Password))).Session!;
            var otherDevice = (await WithAuthServiceAsync(s => s.LoginAsync(email, Password))).Session!;

            var second = (await WithAuthServiceAsync(s => s.RefreshAsync(first.RefreshToken))).Session!;
            var third = (await WithAuthServiceAsync(s => s.RefreshAsync(second.RefreshToken))).Session!;

            // Later (past the grace period for benign races) an attacker replays the first, rotated token.
            _clock.Advance(TimeSpan.FromSeconds(15));
            var reuse = await WithAuthServiceAsync(s => s.RefreshAsync(first.RefreshToken));
            Assert.Equal(AuthStatus.RefreshTokenReused, reuse.Status);

            // Every token of that family is dead, including the newest one the legitimate client holds...
            Assert.Equal(AuthStatus.RefreshTokenReused, (await WithAuthServiceAsync(s => s.RefreshAsync(third.RefreshToken))).Status);
            Assert.Equal(0, await db.ScalarAsync<long>(
                "SELECT COUNT(*) FROM RefreshTokens WHERE FamilyId = (SELECT FamilyId FROM RefreshTokens WHERE TokenHash = @hash) AND RevokedAt IS NULL",
                new { hash = RefreshTokenGenerator.Hash(first.RefreshToken) }));

            // ...while the session started by a separate login is untouched.
            Assert.Equal(AuthStatus.Success, (await WithAuthServiceAsync(s => s.RefreshAsync(otherDevice.RefreshToken))).Status);
        }

        [Fact]
        public async Task ConcurrentRefreshesWithTheSameToken_RotateOnce_AndRefuseTheRest_WithoutRevokingTheFamily()
        {
            var email = await RegisterAsync();
            var token = (await WithAuthServiceAsync(s => s.LoginAsync(email, Password))).Session!.RefreshToken;

            var results = await Task.WhenAll(Enumerable.Range(0, 5)
                .Select(_ => Task.Run(() => WithAuthServiceAsync(s => s.RefreshAsync(token)))));

            Assert.Equal(1, results.Count(r => r.Status == AuthStatus.Success));
            Assert.Equal(4, results.Count(r => r.Status == AuthStatus.InvalidRefreshToken));

            // The winner's new token is the one live token in the family, and it keeps working.
            var winner = results.Single(r => r.Status == AuthStatus.Success).Session!;
            Assert.Equal(1, await ActiveTokensInFamilyAsync(token));
            Assert.Equal(AuthStatus.Success, (await WithAuthServiceAsync(s => s.RefreshAsync(winner.RefreshToken))).Status);
        }

        [Fact]
        public async Task ReusingATokenRevokedByLogout_RevokesTheFamily_EvenWithinTheGracePeriod()
        {
            var email = await RegisterAsync();
            var session = (await WithAuthServiceAsync(s => s.LoginAsync(email, Password))).Session!;
            await WithAuthServiceAsync(async s => { await s.LogoutAsync(session.RefreshToken); return 0; });

            _clock.Advance(TimeSpan.FromSeconds(2));
            var result = await WithAuthServiceAsync(s => s.RefreshAsync(session.RefreshToken));

            Assert.Equal(AuthStatus.RefreshTokenReused, result.Status);
            Assert.Equal(0, await ActiveTokensInFamilyAsync(session.RefreshToken));
        }

        [Fact]
        public async Task Logout_RevokesTheFamily()
        {
            var email = await RegisterAsync();
            var session = (await WithAuthServiceAsync(s => s.LoginAsync(email, Password))).Session!;

            await WithAuthServiceAsync(async s => { await s.LogoutAsync(session.RefreshToken); return 0; });

            Assert.NotEqual(AuthStatus.Success, (await WithAuthServiceAsync(s => s.RefreshAsync(session.RefreshToken))).Status);
        }

        [Fact]
        public async Task Register_WithTakenEmail_ReturnsEmailTaken_EvenInDifferentCase()
        {
            var email = await RegisterAsync();

            var result = await WithAuthServiceAsync(s => s.RegisterAsync(
                new RegisterCommand(email.ToUpperInvariant(), Password, "Other", "User", new DateOnly(1990, 1, 1), null)));

            Assert.Equal(AuthStatus.EmailTaken, result.Status);
        }

        private async Task<string> RegisterAsync()
        {
            var email = MySqlFixture.UniqueEmail();
            var result = await WithAuthServiceAsync(s => s.RegisterAsync(
                new RegisterCommand(email, Password, "Flow", "Tester", new DateOnly(1990, 1, 1), "Female")));
            Assert.Equal(AuthStatus.Success, result.Status);
            return email;
        }

        private Task<long> ActiveTokensInFamilyAsync(string anyTokenInFamily) => db.ScalarAsync<long>(
            "SELECT COUNT(*) FROM RefreshTokens WHERE RevokedAt IS NULL AND FamilyId = (SELECT FamilyId FROM RefreshTokens WHERE TokenHash = @hash)",
            new { hash = RefreshTokenGenerator.Hash(anyTokenInFamily) });

        /// <summary>Runs the action with a fresh unit of work, as one HTTP request would.</summary>
        private async Task<T> WithAuthServiceAsync<T>(Func<AuthService, Task<T>> action)
        {
            await using var uow = db.CreateUnitOfWork();
            var users = new UserRepository(uow);
            var profiles = new ProfileRepository(uow);
            var hasher = new PasswordHasher<User>();
            var userService = new UserService(users, profiles, hasher, _clock, NullLogger<UserService>.Instance);
            var options = JwtAccessTokenServiceTests.Options;
            var service = new AuthService(uow, userService, users, profiles, new RefreshTokenRepository(uow), hasher,
                new JwtAccessTokenService(options, _clock), options, _clock,
                NullLogger<AuthService>.Instance);
            return await action(service);
        }
    }
}
