using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Services;
using RetirementPlanner.Tests.TestSupport;

namespace RetirementPlanner.Tests.Services
{
    public class UserServiceTests
    {
        private const string Email = "jane@example.com";
        private const string Password = "s3cret";

        private readonly Mock<IUserRepository> _userRepo = new();
        private readonly Mock<IProfileRepository> _profileRepo = new();
        private readonly PasswordHasher<User> _hasher = new();
        private readonly User _user;

        public UserServiceTests()
        {
            _user = new User { Id = 7, Email = Email };
            _user.PasswordHash = _hasher.HashPassword(_user, Password);

            _userRepo.Setup(r => r.GetByEmailAsync(Email, It.IsAny<CancellationToken>())).ReturnsAsync(_user);
            _profileRepo.Setup(r => r.GetByUserIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(new UserProfile
            {
                UserId = 7,
                FirstName = "Jane",
                LastName = "Doe",
                DateOfBirth = new DateOnly(1990, 6, 15),
                Gender = "Female"
            });
        }

        private UserService CreateService(DateTimeOffset now) =>
            new(_userRepo.Object, _profileRepo.Object, _hasher, new FixedTimeProvider(now), NullLogger<UserService>.Instance);

        [Fact]
        public async Task AuthenticateAsync_WithCorrectPassword_ReturnsUserWithProfile()
        {
            var user = await CreateService(new DateTimeOffset(2026, 9, 27, 0, 0, 0, TimeSpan.Zero))
                .AuthenticateAsync(Email, Password);

            Assert.NotNull(user);
            Assert.Equal(7, user.UserId);
            Assert.Equal(Email, user.Email);
            Assert.Equal("Jane", user.FirstName);
            Assert.Equal("Doe", user.LastName);
            Assert.Equal(new DateOnly(1990, 6, 15), user.DateOfBirth);
            Assert.Equal("Female", user.Gender);
            Assert.Equal(36, user.Age);
        }

        [Fact]
        public async Task AuthenticateAsync_WithWrongPassword_ReturnsNull()
        {
            var profile = await CreateService(DateTimeOffset.UtcNow).AuthenticateAsync(Email, "wrong");

            Assert.Null(profile);
            _profileRepo.Verify(r => r.GetByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task AuthenticateAsync_WithUnknownEmail_ReturnsNull()
        {
            var profile = await CreateService(DateTimeOffset.UtcNow).AuthenticateAsync("nobody@example.com", Password);

            Assert.Null(profile);
        }

        [Fact]
        public async Task AuthenticateAsync_WhenUserHasNoProfile_ReturnsNull()
        {
            _profileRepo.Setup(r => r.GetByUserIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync((UserProfile?)null);

            var profile = await CreateService(DateTimeOffset.UtcNow).AuthenticateAsync(Email, Password);

            Assert.Null(profile);
        }

        [Theory]
        [InlineData(2026, 6, 14, 35)] // day before the birthday
        [InlineData(2026, 6, 15, 36)] // on the birthday
        [InlineData(2026, 12, 31, 36)]
        public async Task AuthenticateAsync_ComputesAgeFromDateOfBirth(int year, int month, int day, int expectedAge)
        {
            var profile = await CreateService(new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero))
                .AuthenticateAsync(Email, Password);

            Assert.Equal(expectedAge, profile!.Age);
        }

        [Fact]
        public async Task AuthenticateAsync_WhenRepositoryThrows_PropagatesException()
        {
            _userRepo.Setup(r => r.GetByEmailAsync(Email, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("database down"));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateService(DateTimeOffset.UtcNow).AuthenticateAsync(Email, Password));
        }
    }
}
