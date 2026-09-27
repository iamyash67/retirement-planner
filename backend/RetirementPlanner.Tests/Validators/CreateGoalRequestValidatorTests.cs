using FluentValidation.TestHelper;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.Validators;

namespace RetirementPlanner.Tests.Validators
{
    public class CreateGoalRequestValidatorTests
    {
        private readonly CreateGoalRequestValidator _validator = new();

        private static CreateGoalRequest Valid() => new()
        {
            CurrentAge = 30,
            RetirementAge = 60,
            TargetSavings = 1_000_000m,
            CurrentSavings = 100_000m
        };

        [Fact]
        public void ValidRequestWithoutOptionalFields_Passes()
        {
            _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ValidRequestWithOptionalFieldsAtTheirLimits_Passes()
        {
            var request = Valid();
            request.Name = new string('x', CreateGoalRequestValidator.MaxNameLength);
            request.ExpectedAnnualReturn = -1m;
            request.ReturnVolatility = 1m;
            request.InflationRate = -0.5m;
            request.AnnualContributionIncrease = 0m;
            request.CurrentSavings = 0m;
            request.RetirementAge = CreateGoalRequestValidator.MaxAge;

            _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void NonPositiveCurrentAge_Fails()
        {
            var request = Valid();
            request.CurrentAge = 0;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.CurrentAge).WithErrorMessage("Age values must be positive").Only();
        }

        [Fact]
        public void NonPositiveRetirementAge_Fails()
        {
            var request = Valid();
            request.CurrentAge = 1;
            request.RetirementAge = -1;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.RetirementAge).WithErrorMessage("Age values must be positive");
        }

        [Fact]
        public void AgeAboveMaximum_Fails()
        {
            var request = Valid();
            request.CurrentAge = 121;
            request.RetirementAge = 125;

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(r => r.CurrentAge).WithErrorMessage("Age cannot be more than 120");
            result.ShouldHaveValidationErrorFor(r => r.RetirementAge).WithErrorMessage("Age cannot be more than 120");
        }

        [Theory]
        [InlineData(60, 60)]
        [InlineData(60, 55)]
        public void RetirementAgeNotAfterCurrentAge_Fails(int currentAge, int retirementAge)
        {
            var request = Valid();
            request.CurrentAge = currentAge;
            request.RetirementAge = retirementAge;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.RetirementAge)
                .WithErrorMessage("Retirement age must be greater than current age").Only();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public void NonPositiveTargetSavings_FailsWithoutAlsoFlaggingCurrentSavings(decimal target)
        {
            var request = Valid();
            request.TargetSavings = target;
            request.CurrentSavings = 0m;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.TargetSavings).WithErrorMessage("Target savings must be positive").Only();
        }

        [Theory]
        [InlineData(1_000_000)]
        [InlineData(2_000_000)]
        public void CurrentSavingsAtOrAboveTarget_Fails(decimal currentSavings)
        {
            var request = Valid();
            request.CurrentSavings = currentSavings;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.CurrentSavings)
                .WithErrorMessage("You have enough savings to reach your goal").Only();
        }

        [Fact]
        public void NegativeCurrentSavings_Fails()
        {
            var request = Valid();
            request.CurrentSavings = -1m;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.CurrentSavings).WithErrorMessage("Current savings cannot be negative").Only();
        }

        [Fact]
        public void NameTooLong_Fails()
        {
            var request = Valid();
            request.Name = new string('x', CreateGoalRequestValidator.MaxNameLength + 1);

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.Name).WithErrorMessage("Name cannot be longer than 100 characters").Only();
        }

        [Theory]
        [InlineData(1.01)]
        [InlineData(-1.01)]
        public void ExpectedAnnualReturnOutOfRange_Fails(decimal value)
        {
            var request = Valid();
            request.ExpectedAnnualReturn = value;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.ExpectedAnnualReturn)
                .WithErrorMessage("Expected annual return must be between -1 and 1").Only();
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(1.01)]
        public void ReturnVolatilityOutOfRange_Fails(decimal value)
        {
            var request = Valid();
            request.ReturnVolatility = value;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.ReturnVolatility)
                .WithErrorMessage("Return volatility must be between 0 and 1").Only();
        }

        [Theory]
        [InlineData(-0.51)]
        [InlineData(1.01)]
        public void InflationRateOutOfRange_Fails(decimal value)
        {
            var request = Valid();
            request.InflationRate = value;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.InflationRate)
                .WithErrorMessage("Inflation rate must be between -0.5 and 1").Only();
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(1.01)]
        public void AnnualContributionIncreaseOutOfRange_Fails(decimal value)
        {
            var request = Valid();
            request.AnnualContributionIncrease = value;

            _validator.TestValidate(request)
                .ShouldHaveValidationErrorFor(r => r.AnnualContributionIncrease)
                .WithErrorMessage("Annual contribution increase must be between 0 and 1").Only();
        }
    }
}
