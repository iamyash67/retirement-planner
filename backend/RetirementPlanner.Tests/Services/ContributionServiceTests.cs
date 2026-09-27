using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MySqlConnector;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Services;
using RetirementPlanner.Tests.TestSupport;

namespace RetirementPlanner.Tests.Services
{
    public class ContributionServiceTests
    {
        private const int UserId = 3;
        private static readonly DateTimeOffset Now = new(2026, 9, 27, 10, 0, 0, TimeSpan.Zero);

        private readonly Mock<IUnitOfWork> _unitOfWork = UnitOfWorkMock.Create<ContributionResult>();
        private readonly Mock<IGoalRepository> _goalRepo = new();
        private readonly Mock<IContributionRepository> _contributionRepo = new();

        private static readonly RecordContributionCommand Request = new(UserId: UserId, GoalId: 4, Year: 2026, Month: 9, Amount: 500m);

        public ContributionServiceTests()
        {
            _goalRepo.Setup(r => r.GetForUserForUpdateAsync(4, UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Goal { Id = 4, UserId = UserId, TargetAmount = 10_000m, CurrentSavings = 1_000m });
        }

        private ContributionService CreateService() =>
            new(_unitOfWork.Object, _goalRepo.Object, _contributionRepo.Object, new FixedTimeProvider(Now),
                NullLogger<ContributionService>.Instance);

        [Fact]
        public async Task RecordAsync_WithNewMonth_CreatesAndReturnsTheContribution()
        {
            _contributionRepo.Setup(r => r.CreateAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>())).ReturnsAsync(42);

            var result = await CreateService().RecordAsync(Request);

            Assert.Equal(ContributionStatus.Recorded, result.Status);
            Assert.NotNull(result.Contribution);
            Assert.Equal(42, result.Contribution.Id);
            Assert.Equal(Now.UtcDateTime, result.Contribution.RecordedAt);
            _contributionRepo.Verify(r => r.CreateAsync(
                It.Is<Contribution>(c => c.GoalId == 4 && c.Year == 2026 && c.Month == 9 && c.Amount == 500m && c.RecordedAt == Now.UtcDateTime),
                It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<ContributionResult>>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RecordAsync_WhenGoalMissingOrOwnedByAnotherUser_ReturnsGoalNotFound()
        {
            // The repository only finds goals owned by the given user, so another user's goal looks missing.
            var result = await CreateService().RecordAsync(Request with { UserId = 99 });

            Assert.Equal(ContributionStatus.GoalNotFound, result.Status);
            _goalRepo.Verify(r => r.GetForUserForUpdateAsync(4, 99, It.IsAny<CancellationToken>()), Times.Once);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task RecordAsync_WhenAmountExceedsTarget_ReturnsExceedsTarget()
        {
            var result = await CreateService().RecordAsync(Request with { Amount = 10_000.01m });

            Assert.Equal(ContributionStatus.ExceedsTarget, result.Status);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task RecordAsync_WhenMonthAlreadyRecorded_ReturnsAlreadyRecorded()
        {
            _contributionRepo.Setup(r => r.ExistsAsync(4, 2026, 9, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateService().RecordAsync(Request);

            Assert.Equal(ContributionStatus.AlreadyRecorded, result.Status);
            Assert.Null(result.Contribution);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task RecordAsync_WhenRepositoryThrows_PropagatesExceptionOutOfTheTransaction()
        {
            _contributionRepo.Setup(r => r.CreateAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("insert failed"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService().RecordAsync(Request));
        }

        [Fact]
        public async Task RecordAsync_WhenInsertHitsUniqueKey_ReturnsAlreadyRecorded()
        {
            // Another request committed the same month between the duplicate check and the insert.
            _contributionRepo.Setup(r => r.CreateAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(MySqlExceptionFactory.Create(MySqlErrorCode.DuplicateKeyEntry, "Duplicate entry"));

            var result = await CreateService().RecordAsync(Request);

            Assert.Equal(ContributionStatus.AlreadyRecorded, result.Status);
            Assert.Null(result.Contribution);
        }

        [Fact]
        public async Task RecordAsync_WhenInsertFailsWithOtherMySqlError_Propagates()
        {
            var error = MySqlExceptionFactory.Create(MySqlErrorCode.LockWaitTimeout, "Lock wait timeout");
            _contributionRepo.Setup(r => r.CreateAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(error);

            var thrown = await Assert.ThrowsAsync<MySqlException>(() => CreateService().RecordAsync(Request));

            Assert.Same(error, thrown);
        }

        private void VerifyNothingCreated() =>
            _contributionRepo.Verify(r => r.CreateAsync(It.IsAny<Contribution>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
