using FluentValidation;
using RetirementPlanner.DTO.Requests;

namespace RetirementPlanner.Validators
{
    public class AddContributionRequestValidator : AbstractValidator<AddContributionRequest>
    {
        public const int FirstYear = 1980;

        public AddContributionRequestValidator(TimeProvider timeProvider)
        {
            // The current year is read on every validation, not when the validator is created.
            RuleFor(r => r.Year)
                .Must(year => year >= FirstYear && year <= timeProvider.GetLocalNow().Year)
                .WithMessage("Invalid Year");

            RuleFor(r => r.Month).InclusiveBetween(1, 12).WithMessage("Month must be between 1-12");

            RuleFor(r => r.Amount).GreaterThan(0).WithMessage("Amount must be positive");
        }
    }
}
