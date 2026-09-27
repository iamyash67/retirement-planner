using FluentValidation.TestHelper;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.Tests.TestSupport;
using RetirementPlanner.Validators;

namespace RetirementPlanner.Tests.Validators
{
    public class AddInvestmentRequestValidatorTests
    {
        private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);
        private readonly AddInvestmentRequestValidator _validator = new(new FixedTimeProvider(Now));

        private static AddInvestmentRequest Valid() => new() { GoalId = 1, Year = 2026, Month = 9, MonthlyInvestment = 500m };

        [Fact]
        public void ValidRequest_Passes()
        {
            _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void NonPositiveGoalId_Fails(int goalId)
        {
            var request = Valid();
            request.GoalId = goalId;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.GoalId).WithErrorMessage("Invalid Goal ID").Only();
        }

        [Theory]
        [InlineData(1980)]
        [InlineData(2026)]
        public void YearWithinRange_Passes(int year)
        {
            var request = Valid();
            request.Year = year;

            _validator.TestValidate(request).ShouldNotHaveValidationErrorFor(r => r.Year);
        }

        [Theory]
        [InlineData(1979)]
        [InlineData(2027)]
        public void YearOutsideRange_Fails(int year)
        {
            var request = Valid();
            request.Year = year;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.Year).WithErrorMessage("Invalid Year").Only();
        }

        [Fact]
        public void Year_UsesTheInjectedTimeProvider()
        {
            var request = Valid();
            request.Year = 2031;

            new AddInvestmentRequestValidator(new FixedTimeProvider(new DateTimeOffset(2031, 1, 1, 12, 0, 0, TimeSpan.Zero)))
                .TestValidate(request)
                .ShouldNotHaveValidationErrorFor(r => r.Year);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public void MonthOutsideOneToTwelve_Fails(int month)
        {
            var request = Valid();
            request.Month = month;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.Month).WithErrorMessage("Month must be between 1-12").Only();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void NonPositiveMonthlyInvestment_Fails(decimal amount)
        {
            var request = Valid();
            request.MonthlyInvestment = amount;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.MonthlyInvestment)
                .WithErrorMessage("Monthly investment must be positive").Only();
        }
    }
}
