using FluentValidation;
using RetirementPlanner.DTO.Requests;

namespace RetirementPlanner.Validators
{
    /// <summary>Only presence is checked: format and length rules belong to registration, not to signing in.</summary>
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(r => r.Email).NotEmpty().WithMessage("Email is required");
            RuleFor(r => r.Password).NotEmpty().WithMessage("Password is required");
        }
    }
}
