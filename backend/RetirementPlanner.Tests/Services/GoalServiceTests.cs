using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Services;
using RetirementPlanner.Tests.TestSupport;

namespace RetirementPlanner.Tests.Services
{
    public class GoalServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = UnitOfWorkMock.Create<CreateGoalResult>();
        private readonly Mock<IGoalRepository> _goalRepo = new();
        private readonly Mock<IUserRepository> _userRepo = new();

        private GoalService CreateService() =>
            new(_unitOfWork.Object, _goalRepo.Object, _userRepo.Object, NullLogger<GoalService>.Instance);

        private static CreateGoalCommand ValidCommand() => new(
            UserId: 3, CurrentAge: 30, RetirementAge: 60, TargetAmount: 1_000_000m, CurrentSavings: 100_000m);

        [Fact]
        public async Task CreateGoalAsync_WithValidCommand_CreatesGoalWithDefaultsAndPlannedContribution()
        {
            _userRepo.Setup(r => r.TryLockAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            NewGoal? saved = null;
            _goalRepo.Setup(r => r.CreateAsync(It.IsAny<NewGoal>(), It.IsAny<CancellationToken>()))
                .Callback<NewGoal, CancellationToken>((g, _) => saved = g)
                .ReturnsAsync(11);
            var created = new Goal { Id = 11, UserId = 3 };
            _goalRepo.Setup(r => r.GetForUserAsync(11, 3, It.IsAny<CancellationToken>())).ReturnsAsync(created);

            var result = await CreateService().CreateGoalAsync(ValidCommand());

            Assert.Equal(GoalCreationStatus.Created, result.Status);
            Assert.Same(created, result.Goal);
            Assert.NotNull(saved);
            Assert.Equal(3, saved.UserId);
            Assert.Equal(GoalService.DefaultName, saved.Name);
            Assert.Equal(1_000_000m, saved.TargetAmount);
            Assert.Equal(100_000m, saved.CurrentSavings);
            Assert.Equal(GoalService.DefaultExpectedAnnualReturn, saved.ExpectedAnnualReturn);
            Assert.Equal(GoalService.DefaultReturnVolatility, saved.ReturnVolatility);
            Assert.Equal(GoalService.DefaultInflationRate, saved.InflationRate);
            Assert.Equal(GoalService.DefaultAnnualContributionIncrease, saved.AnnualContributionIncrease);
            Assert.Equal(2500m, saved.PlannedMonthlyContribution); // 900,000 over 360 months
            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<CreateGoalResult>>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateGoalAsync_UsesProvidedSimulationInputs()
        {
            _userRepo.Setup(r => r.TryLockAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            NewGoal? saved = null;
            _goalRepo.Setup(r => r.CreateAsync(It.IsAny<NewGoal>(), It.IsAny<CancellationToken>()))
                .Callback<NewGoal, CancellationToken>((g, _) => saved = g)
                .ReturnsAsync(11);
            _goalRepo.Setup(r => r.GetForUserAsync(11, 3, It.IsAny<CancellationToken>())).ReturnsAsync(new Goal { Id = 11 });
            var command = ValidCommand() with
            {
                Name = "  Early retirement ",
                ExpectedAnnualReturn = 0.07m,
                ReturnVolatility = 0.15m,
                InflationRate = 0.03m,
                AnnualContributionIncrease = 0.02m
            };

            await CreateService().CreateGoalAsync(command);

            Assert.Equal("Early retirement", saved!.Name);
            Assert.Equal(0.07m, saved.ExpectedAnnualReturn);
            Assert.Equal(0.15m, saved.ReturnVolatility);
            Assert.Equal(0.03m, saved.InflationRate);
            Assert.Equal(0.02m, saved.AnnualContributionIncrease);
        }

        [Fact]
        public async Task CreateGoalAsync_WhenUserDoesNotExist_ReturnsUserNotFound()
        {
            _userRepo.Setup(r => r.TryLockAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            var result = await CreateService().CreateGoalAsync(ValidCommand());

            Assert.Equal(GoalCreationStatus.UserNotFound, result.Status);
            Assert.Null(result.Goal);
            _goalRepo.Verify(r => r.CreateAsync(It.IsAny<NewGoal>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateGoalAsync_WhenUserAlreadyHasGoal_ReturnsAlreadyExists()
        {
            _userRepo.Setup(r => r.TryLockAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _goalRepo.Setup(r => r.ExistsForUserAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateService().CreateGoalAsync(ValidCommand());

            Assert.Equal(GoalCreationStatus.AlreadyExists, result.Status);
            Assert.Null(result.Goal);
            _goalRepo.Verify(r => r.CreateAsync(It.IsAny<NewGoal>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Theory]
        [InlineData(1000, 0, 30, 31, 83.33)]       // 83.333… rounds down
        [InlineData(1000.2, 1000, 30, 31, 0.02)]   // 0.01666… rounds up
        [InlineData(10, 0, 40, 41, 0.83)]
        [InlineData(0.06, 0, 30, 31, 0.01)]        // 0.005 is a midpoint: rounds away from zero, like MySQL ROUND
        public void CalculateMonthlyContribution_RoundsLikeMySql(
            decimal target, decimal current, int currentAge, int retirementAge, decimal expected)
        {
            Assert.Equal(expected, GoalService.CalculateMonthlyContribution(target, current, currentAge, retirementAge));
        }

        [Fact]
        public void CalculateMonthlyContribution_WhenRetirementAgeNotAfterCurrentAge_Throws()
        {
            Assert.Throws<ArgumentException>(() => GoalService.CalculateMonthlyContribution(1000, 0, 60, 60));
        }

        [Fact]
        public async Task GetProgressAsync_ReturnsCurrentSavingsAsPercentageOfTarget()
        {
            _goalRepo.Setup(r => r.GetForUserAsync(5, 3, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Goal { Id = 5, TargetAmount = 200_000m, CurrentSavings = 50_000m });

            var progress = await CreateService().GetProgressAsync(userId: 3, goalId: 5);

            Assert.Equal(25m, progress);
        }

        [Fact]
        public async Task GetProgressAsync_WhenGoalMissingOrOwnedByAnotherUser_ReturnsNull()
        {
            _goalRepo.Setup(r => r.GetForUserAsync(5, 3, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Goal { Id = 5, TargetAmount = 100m });

            Assert.Null(await CreateService().GetProgressAsync(userId: 4, goalId: 5));
        }

        [Fact]
        public async Task GetProgressAsync_WhenTargetIsZero_ReturnsNull()
        {
            _goalRepo.Setup(r => r.GetForUserAsync(5, 3, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Goal { Id = 5, TargetAmount = 0m, CurrentSavings = 10m });

            Assert.Null(await CreateService().GetProgressAsync(userId: 3, goalId: 5));
        }

        [Fact]
        public async Task ListGoalsAsync_ReturnsTheUsersGoals()
        {
            IReadOnlyList<Goal> goals = [new Goal { Id = 9, UserId = 3 }];
            _goalRepo.Setup(r => r.ListByUserIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(goals);

            Assert.Same(goals, await CreateService().ListGoalsAsync(3));
        }

        [Fact]
        public async Task GetGoalAsync_LooksUpByGoalAndOwner()
        {
            var goal = new Goal { Id = 9, UserId = 3 };
            _goalRepo.Setup(r => r.GetForUserAsync(9, 3, It.IsAny<CancellationToken>())).ReturnsAsync(goal);

            Assert.Same(goal, await CreateService().GetGoalAsync(userId: 3, goalId: 9));
            Assert.Null(await CreateService().GetGoalAsync(userId: 4, goalId: 9));
        }
    }
}
