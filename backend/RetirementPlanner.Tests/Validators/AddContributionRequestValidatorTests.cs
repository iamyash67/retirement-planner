using FluentValidation.TestHelper;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.Tests.TestSupport;
using RetirementPlanner.Validators;

namespace RetirementPlanner.Tests.Validators
{
    public class AddContributionRequestValidatorTests
    {
        private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);
        private readonly AddContributionRequestValidator _validator = new(new FixedTimeProvider(Now));

        private static AddContributionRequest Valid() => new() { Year = 2026, Month = 9, Amount = 500m };

        [Fact]
        public void ValidRequest_Passes()
        {
            _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
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

            new AddContributionRequestValidator(new FixedTimeProvider(new DateTimeOffset(2031, 1, 1, 12, 0, 0, TimeSpan.Zero)))
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
        public void NonPositiveAmount_Fails(decimal amount)
        {
            var request = Valid();
            request.Amount = amount;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.Amount).WithErrorMessage("Amount must be positive").Only();
        }
    }
}
