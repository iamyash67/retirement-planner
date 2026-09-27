using FluentValidation;
using RetirementPlanner.DTO.Requests;

namespace RetirementPlanner.Validators
{
    public class GetProgressRequestValidator : AbstractValidator<GetProgressRequest>
    {
        public GetProgressRequestValidator()
        {
            RuleFor(r => r.GoalId).GreaterThan(0).WithMessage("Invalid Goal ID");
        }
    }
}
