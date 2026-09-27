using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RetirementPlanner.Data.Interfaces;
using RetirementPlanner.DTO;
using RetirementPlanner.Models;
using RetirementPlanner.Repositories.Interfaces;
using RetirementPlanner.Services;
using RetirementPlanner.Tests.TestSupport;

namespace RetirementPlanner.Tests.Services
{
    public class GoalServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWork = UnitOfWorkMock.Create<GoalCreationResult>();
        private readonly Mock<IGoalRepository> _goalRepo = new();
        private readonly Mock<IUserRepository> _userRepo = new();

        private GoalService CreateService() =>
            new(_unitOfWork.Object, _goalRepo.Object, _userRepo.Object, NullLogger<GoalService>.Instance);

        private static GoalDTO ValidRequest() => new()
        {
            ProfileId = 3,
            CurrentAge = 30,
            RetirementAge = 60,
            TargetSavings = 1_000_000m,
            CurrentSavings = 100_000m
        };

        [Fact]
        public async Task CreateGoalAsync_WithValidRequest_CreatesGoalWithDefaultsAndPlannedContribution()
        {
            _userRepo.Setup(r => r.TryLockAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            NewGoal? saved = null;
            _goalRepo.Setup(r => r.CreateAsync(It.IsAny<NewGoal>(), It.IsAny<CancellationToken>()))
                .Callback<NewGoal, CancellationToken>((g, _) => saved = g)
                .ReturnsAsync(11);

            var result = await CreateService().CreateGoalAsync(ValidRequest());

            Assert.Equal(GoalCreationResult.Created, result);
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
            _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<GoalCreationResult>>>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateGoalAsync_UsesProvidedSimulationInputs()
        {
            _userRepo.Setup(r => r.TryLockAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            NewGoal? saved = null;
            _goalRepo.Setup(r => r.CreateAsync(It.IsAny<NewGoal>(), It.IsAny<CancellationToken>()))
                .Callback<NewGoal, CancellationToken>((g, _) => saved = g)
                .ReturnsAsync(11);
            var request = ValidRequest();
            request.Name = "  Early retirement ";
            request.ExpectedAnnualReturn = 0.07m;
            request.ReturnVolatility = 0.15m;
            request.InflationRate = 0.03m;
            request.AnnualContributionIncrease = 0.02m;

            await CreateService().CreateGoalAsync(request);

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

            var result = await CreateService().CreateGoalAsync(ValidRequest());

            Assert.Equal(GoalCreationResult.UserNotFound, result);
            _goalRepo.Verify(r => r.CreateAsync(It.IsAny<NewGoal>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateGoalAsync_WhenUserAlreadyHasGoal_ReturnsAlreadyExists()
        {
            _userRepo.Setup(r => r.TryLockAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _goalRepo.Setup(r => r.ExistsForUserAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(true);

            var result = await CreateService().CreateGoalAsync(ValidRequest());

            Assert.Equal(GoalCreationResult.AlreadyExists, result);
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
            _goalRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Goal { GoalId = 5, TargetSavings = 200_000m, CurrentSavings = 50_000m });

            var progress = await CreateService().GetProgressAsync(5);

            Assert.Equal(25m, progress);
        }

        [Fact]
        public async Task GetProgressAsync_WhenGoalMissing_ReturnsNull()
        {
            Assert.Null(await CreateService().GetProgressAsync(5));
        }

        [Fact]
        public async Task GetProgressAsync_WhenTargetIsZero_ReturnsNull()
        {
            _goalRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Goal { GoalId = 5, TargetSavings = 0m, CurrentSavings = 10m });

            Assert.Null(await CreateService().GetProgressAsync(5));
        }

        [Fact]
        public async Task GetGoalForUserAsync_LooksUpByUserId()
        {
            var goal = new Goal { GoalId = 9, ProfileId = 3 };
            _goalRepo.Setup(r => r.GetLatestByUserIdAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(goal);

            Assert.Same(goal, await CreateService().GetGoalForUserAsync(3));
        }
    }
}
