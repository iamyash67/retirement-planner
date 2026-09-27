using FluentValidation;
using RetirementPlanner.DTO.Requests;

namespace RetirementPlanner.Validators
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(r => r.UserName).NotEmpty().WithMessage("Username is required");
            RuleFor(r => r.Password).NotEmpty().WithMessage("Password is required");
        }
    }
}
