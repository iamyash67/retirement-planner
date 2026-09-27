using FluentValidation;
using RetirementPlanner.DTO.Requests;

namespace RetirementPlanner.Validators
{
    public class GetGoalRequestValidator : AbstractValidator<GetGoalRequest>
    {
        public GetGoalRequestValidator()
        {
            RuleFor(r => r.ProfileId).GreaterThan(0).WithMessage("Invalid Profile ID");
        }
    }
}
