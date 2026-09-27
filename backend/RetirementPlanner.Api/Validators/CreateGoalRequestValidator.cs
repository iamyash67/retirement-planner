using FluentValidation;
using RetirementPlanner.DTO.Requests;

namespace RetirementPlanner.Validators
{
    /// <summary>
    /// The messages match the checks the controller used to make. The upper bounds mirror the CHECK
    /// constraints on the Goals table, so an out-of-range value is a 400 and not a database error.
    /// </summary>
    public class CreateGoalRequestValidator : AbstractValidator<CreateGoalRequest>
    {
        public const int MaxAge = 120;
        public const int MaxNameLength = 100;

        public CreateGoalRequestValidator()
        {
            RuleFor(r => r.CurrentAge)
                .GreaterThan(0).WithMessage("Age values must be positive")
                .LessThanOrEqualTo(MaxAge).WithMessage($"Age cannot be more than {MaxAge}");

            RuleFor(r => r.RetirementAge)
                .GreaterThan(0).WithMessage("Age values must be positive")
                .LessThanOrEqualTo(MaxAge).WithMessage($"Age cannot be more than {MaxAge}")
                .GreaterThan(r => r.CurrentAge).WithMessage("Retirement age must be greater than current age");

            RuleFor(r => r.TargetSavings).GreaterThan(0).WithMessage("Target savings must be positive");

            RuleFor(r => r.CurrentSavings)
                .GreaterThanOrEqualTo(0).WithMessage("Current savings cannot be negative")
                .LessThan(r => r.TargetSavings).WithMessage("You have enough savings to reach your goal")
                .When(r => r.TargetSavings > 0, ApplyConditionTo.CurrentValidator);

            RuleFor(r => r.Name)
                .MaximumLength(MaxNameLength).WithMessage($"Name cannot be longer than {MaxNameLength} characters");

            RuleFor(r => r.ExpectedAnnualReturn)
                .InclusiveBetween(-1m, 1m).WithMessage("Expected annual return must be between -1 and 1");
            RuleFor(r => r.ReturnVolatility)
                .InclusiveBetween(0m, 1m).WithMessage("Return volatility must be between 0 and 1");
            RuleFor(r => r.InflationRate)
                .InclusiveBetween(-0.5m, 1m).WithMessage("Inflation rate must be between -0.5 and 1");
            RuleFor(r => r.AnnualContributionIncrease)
                .InclusiveBetween(0m, 1m).WithMessage("Annual contribution increase must be between 0 and 1");
        }
    }
}
