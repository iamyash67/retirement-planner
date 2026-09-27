using Microsoft.AspNetCore.Identity;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;

namespace RetirementPlanner.Data
{
    /// <summary>
    /// Creates the demo login (demo@example.com / demo123) for local development.
    /// This lives in code rather than in a migration, so demo credentials never reach other environments.
    /// </summary>
    public class DevelopmentDataSeeder : IDataSeeder
    {
        public const string DemoEmail = "demo@example.com";
        public const string DemoPassword = "demo123";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserRepository _userRepo;
        private readonly IProfileRepository _profileRepo;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<DevelopmentDataSeeder> _logger;

        public DevelopmentDataSeeder(
            IUnitOfWork unitOfWork,
            IUserRepository userRepo,
            IProfileRepository profileRepo,
            IPasswordHasher<User> passwordHasher,
            ILogger<DevelopmentDataSeeder> logger)
        {
            _unitOfWork = unitOfWork;
            _userRepo = userRepo;
            _profileRepo = profileRepo;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public Task SeedAsync(CancellationToken cancellationToken = default) =>
            _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (await _userRepo.GetByEmailAsync(DemoEmail, cancellationToken) != null)
                    return false;

                var user = new User { Email = DemoEmail };
                var userId = await _userRepo.CreateAsync(
                    DemoEmail, _passwordHasher.HashPassword(user, DemoPassword), cancellationToken);

                await _profileRepo.CreateAsync(new UserProfile
                {
                    UserId = userId,
                    FirstName = "Demo",
                    LastName = "User",
                    DateOfBirth = new DateOnly(1996, 1, 1),
                    Gender = "Male"
                }, cancellationToken);

                _logger.LogInformation("Seeded demo user {Email}", DemoEmail);
                return true;
            }, cancellationToken);
    }
}
