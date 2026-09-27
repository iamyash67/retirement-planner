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
        private readonly Mock<IUnitOfWork> _unitOfWork = UnitOfWorkMock.Create<ContributionResult>();
        private readonly Mock<IGoalRepository> _goalRepo = new();
        private readonly Mock<IContributionRepository> _contributionRepo = new();

        private static readonly RecordContributionCommand Request = new(GoalId: 4, Year: 2026, Month: 9, Amount: 500m);

        public ContributionServiceTests()
        {
            _goalRepo.Setup(r => r.GetByIdForUpdateAsync(4, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Goal { Id = 4, TargetAmount = 10_000m, CurrentSavings = 1_000m });
        }

        private ContributionService CreateService() =>
            new(_unitOfWork.Object, _goalRepo.Object, _contributionRepo.Object, NullLogger<ContributionService>.Instance);

        [Fact]
        public async Task RecordAsync_WithNewMonth_CreatesContributionAndReturnsUpdatedGoal()
        {
            var updated = new Goal { Id = 4, TargetAmount = 10_000m, CurrentSavings = 1_500m };
            _goalRepo.Setup(r => r.GetByIdAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync(updated);

            var result = await CreateService().RecordAsync(Request);

            Assert.Equal(ContributionStatus.Recorded, result.Status);
            Assert.Same(updated, result.Goal);
            _contributionRepo.Verify(r => r.CreateAsync(
                It.Is<Contribution>(c => c.GoalId == 4 && c.Year == 2026 && c.Month == 9 && c.Amount == 500m),
                It.IsAny<CancellationToken>()), Times.Once);
            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<ContributionResult>>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RecordAsync_WhenGoalMissing_ReturnsGoalNotFound()
        {
            _goalRepo.Setup(r => r.GetByIdForUpdateAsync(4, It.IsAny<CancellationToken>())).ReturnsAsync((Goal?)null);

            var result = await CreateService().RecordAsync(Request);

            Assert.Equal(ContributionStatus.GoalNotFound, result.Status);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task RecordAsync_WhenAmountExceedsTarget_ReturnsExceedsTarget()
        {
            var result = await CreateService().RecordAsync(
                new RecordContributionCommand(GoalId: 4, Year: 2026, Month: 9, Amount: 10_000.01m));

            Assert.Equal(ContributionStatus.ExceedsTarget, result.Status);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task RecordAsync_WhenMonthAlreadyRecorded_ReturnsAlreadyRecorded()
        {
            _contributionRepo.Setup(r => r.ExistsAsync(4, 2026, 9, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateService().RecordAsync(Request);

            Assert.Equal(ContributionStatus.AlreadyRecorded, result.Status);
            Assert.Null(result.Goal);
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
            Assert.Null(result.Goal);
            _goalRepo.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
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
