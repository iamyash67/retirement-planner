using FluentValidation;
using RetirementPlanner.DTO.Requests;

namespace RetirementPlanner.Validators
{
    public class AddInvestmentRequestValidator : AbstractValidator<AddInvestmentRequest>
    {
        public const int FirstYear = 1980;

        public AddInvestmentRequestValidator(TimeProvider timeProvider)
        {
            RuleFor(r => r.GoalId).GreaterThan(0).WithMessage("Invalid Goal ID");

            // The current year is read on every validation, not when the validator is created.
            RuleFor(r => r.Year)
                .Must(year => year >= FirstYear && year <= timeProvider.GetLocalNow().Year)
                .WithMessage("Invalid Year");

            RuleFor(r => r.Month).InclusiveBetween(1, 12).WithMessage("Month must be between 1-12");

            RuleFor(r => r.MonthlyInvestment).GreaterThan(0).WithMessage("Monthly investment must be positive");
        }
    }
}
