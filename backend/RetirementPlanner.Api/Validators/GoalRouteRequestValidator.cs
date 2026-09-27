using FluentValidation;
using RetirementPlanner.DTO.Requests;

namespace RetirementPlanner.Validators
{
    public class GoalRouteRequestValidator : AbstractValidator<GoalRouteRequest>
    {
        public GoalRouteRequestValidator()
        {
            RuleFor(r => r.Id).GreaterThan(0).WithMessage("Invalid Goal ID");
        }
    }
}
