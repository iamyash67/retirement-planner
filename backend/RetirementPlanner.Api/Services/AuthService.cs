using Microsoft.AspNetCore.Identity;
using MySqlConnector;
using RetirementPlanner.Auth;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Services.Interfaces;

namespace RetirementPlanner.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepo;
        private readonly IProfileRepository _profileRepo;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IAccessTokenService _accessTokens;
        private readonly JwtOptions _jwtOptions;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork unitOfWork,
            IUserService userService,
            IUserRepository userRepo,
            IProfileRepository profileRepo,
            IRefreshTokenRepository refreshTokenRepo,
            IPasswordHasher<User> passwordHasher,
            IAccessTokenService accessTokens,
            JwtOptions jwtOptions,
            TimeProvider timeProvider,
            ILogger<AuthService> logger)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
            _userRepo = userRepo;
            _profileRepo = profileRepo;
            _refreshTokenRepo = refreshTokenRepo;
            _passwordHasher = passwordHasher;
            _accessTokens = accessTokens;
            _jwtOptions = jwtOptions;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<AuthResult> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    if (await _userRepo.GetByEmailAsync(command.Email, cancellationToken) != null)
                        return new AuthResult(AuthStatus.EmailTaken);

                    var hash = _passwordHasher.HashPassword(new User { Email = command.Email }, command.Password);
                    var userId = await _userRepo.CreateAsync(command.Email, hash, cancellationToken);
                    await _profileRepo.CreateAsync(new UserProfile
                    {
                        UserId = userId,
                        FirstName = command.FirstName,
                        LastName = command.LastName,
                        DateOfBirth = command.DateOfBirth,
                        Gender = command.Gender
                    }, cancellationToken);

                    var user = await _userService.GetAsync(userId, cancellationToken)
                        ?? throw new InvalidOperationException($"User {userId} disappeared inside its own transaction.");

                    _logger.LogInformation("Registered user {UserId}", userId);
                    var (session, _) = await StartSessionAsync(user, NewFamilyId(), cancellationToken);
                    return new AuthResult(AuthStatus.Success, session);
                }, cancellationToken);
            }
            catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
            {
                // A concurrent registration took the email between the check and the insert (UQ_Users_Email).
                return new AuthResult(AuthStatus.EmailTaken);
            }
        }

        public async Task<AuthResult> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var user = await _userService.AuthenticateAsync(email, password, cancellationToken);
            if (user == null)
                return new AuthResult(AuthStatus.InvalidCredentials);

            var (session, _) = await StartSessionAsync(user, NewFamilyId(), cancellationToken);
            return new AuthResult(AuthStatus.Success, session);
        }

        public async Task<AuthResult> RefreshAsync(string? refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return new AuthResult(AuthStatus.InvalidRefreshToken);

            var tokenHash = RefreshTokenGenerator.Hash(refreshToken);

            // One transaction with the token row locked: two refreshes with the same token are serialised, so
            // exactly one rotates it and the other sees it revoked. The family revocation on reuse is committed.
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var stored = await _refreshTokenRepo.GetByHashForUpdateAsync(tokenHash, cancellationToken);
                if (stored == null)
                    return new AuthResult(AuthStatus.InvalidRefreshToken);

                var now = UtcNow();

                if (stored.RevokedAt != null)
                {
                    if (IsWithinRotationGracePeriod(stored, now))
                    {
                        // Rotated moments ago: almost certainly another tab or request that refreshed with the same
                        // cookie at the same time. Refuse it, but keep the session the winner just continued.
                        _logger.LogInformation(
                            "Refresh token for user {UserId} was rotated {Seconds:0.0}s ago; refusing without revoking family {FamilyId}",
                            stored.UserId, (now - stored.RevokedAt.Value).TotalSeconds, stored.FamilyId);
                        return new AuthResult(AuthStatus.InvalidRefreshToken);
                    }

                    // Reused after the grace period, or revoked by logout or an earlier family revocation: either it
                    // was stolen or it is being replayed. Either way nobody should keep using this session.
                    var revoked = await _refreshTokenRepo.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
                    _logger.LogWarning(
                        "Refresh token reuse detected for user {UserId}; revoked {Count} token(s) in family {FamilyId}",
                        stored.UserId, revoked, stored.FamilyId);
                    return new AuthResult(AuthStatus.RefreshTokenReused);
                }

                if (stored.ExpiresAt <= now)
                    return new AuthResult(AuthStatus.InvalidRefreshToken);

                var user = await _userService.GetAsync(stored.UserId, cancellationToken);
                if (user == null)
                    return new AuthResult(AuthStatus.InvalidRefreshToken);

                // Rotation: the new token joins the same family and the presented one is revoked, pointing at it.
                var (session, newTokenId) = await StartSessionAsync(user, stored.FamilyId, cancellationToken);
                await _refreshTokenRepo.RevokeAsync(stored.Id, now, newTokenId, cancellationToken);
                return new AuthResult(AuthStatus.Success, session);
            }, cancellationToken);
        }

        public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return;

            var tokenHash = RefreshTokenGenerator.Hash(refreshToken);
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var stored = await _refreshTokenRepo.GetByHashForUpdateAsync(tokenHash, cancellationToken);
                if (stored != null)
                    await _refreshTokenRepo.RevokeFamilyAsync(stored.FamilyId, UtcNow(), cancellationToken);
                return stored != null;
            }, cancellationToken);
        }

        /// <summary>Stores a new refresh token in the family and issues an access token for the user.</summary>
        private async Task<(AuthSession Session, int RefreshTokenId)> StartSessionAsync(
            AuthenticatedUser user, Guid familyId, CancellationToken cancellationToken)
        {
            var now = UtcNow();
            var expiresAt = now.AddDays(_jwtOptions.RefreshTokenDays);
            var (token, hash) = RefreshTokenGenerator.Create();

            var tokenId = await _refreshTokenRepo.CreateAsync(new RefreshToken
            {
                UserId = user.UserId,
                FamilyId = familyId,
                TokenHash = hash,
                CreatedAt = now,
                ExpiresAt = expiresAt
            }, cancellationToken);

            var session = new AuthSession(user, _accessTokens.CreateAccessToken(user), token,
                new DateTimeOffset(expiresAt, TimeSpan.Zero));
            return (session, tokenId);
        }

        /// <summary>
        /// True only for a token revoked by rotation (it has a replacement) no more than the grace period ago.
        /// Tokens revoked by logout or family revocation have no replacement and never qualify.
        /// </summary>
        private bool IsWithinRotationGracePeriod(RefreshToken token, DateTime now) =>
            token.ReplacedByTokenId != null
            && token.RevokedAt != null
            && now - token.RevokedAt.Value <= TimeSpan.FromSeconds(_jwtOptions.RefreshTokenReuseGraceSeconds);

        private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

        private static Guid NewFamilyId() => Guid.NewGuid();
    }
}
