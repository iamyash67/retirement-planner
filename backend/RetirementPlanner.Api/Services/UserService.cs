using Microsoft.AspNetCore.Identity;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Services.Interfaces;

namespace RetirementPlanner.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IProfileRepository _profileRepo;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository userRepo,
            IProfileRepository profileRepo,
            IPasswordHasher<User> passwordHasher,
            TimeProvider timeProvider,
            ILogger<UserService> logger)
        {
            _userRepo = userRepo;
            _profileRepo = profileRepo;
            _passwordHasher = passwordHasher;
            _timeProvider = timeProvider;
            _logger = logger;
        }

        public async Task<AuthenticatedUser?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.GetByEmailAsync(email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Login failed: no user with email {Email}", email);
                return null;
            }

            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Login failed: wrong password for user {UserId}", user.Id);
                return null;
            }

            var authenticated = await WithProfileAsync(user, cancellationToken);
            if (authenticated == null)
                _logger.LogWarning("Login failed: user {UserId} has no profile", user.Id);
            return authenticated;
        }

        public async Task<AuthenticatedUser?> GetAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepo.GetByIdAsync(userId, cancellationToken);
            return user == null ? null : await WithProfileAsync(user, cancellationToken);
        }

        private async Task<AuthenticatedUser?> WithProfileAsync(User user, CancellationToken cancellationToken)
        {
            var profile = await _profileRepo.GetByUserIdAsync(user.Id, cancellationToken);
            if (profile == null)
                return null;

            var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
            return new AuthenticatedUser
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                DateOfBirth = profile.DateOfBirth,
                Age = CalculateAge(profile.DateOfBirth, today),
                Gender = profile.Gender
            };
        }

        private static int CalculateAge(DateOnly dateOfBirth, DateOnly today)
        {
            var age = today.Year - dateOfBirth.Year;
            return dateOfBirth > today.AddYears(-age) ? age - 1 : age;
        }
    }
}
