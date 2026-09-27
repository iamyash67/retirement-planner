using FluentValidation;
using RetirementPlanner.DTO.Requests;

namespace RetirementPlanner.Validators
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public const int MinPasswordLength = 8;
        public const int MaxPasswordLength = 128;
        public const int MaxEmailLength = 255;
        public const int MaxNameLength = 100;
        public const int MaxGenderLength = 30;
        public static readonly DateOnly EarliestDateOfBirth = new(1900, 1, 1);

        public RegisterRequestValidator(TimeProvider timeProvider)
        {
            RuleFor(r => r.Email)
                .NotEmpty().WithMessage("Email is required")
                .MaximumLength(MaxEmailLength).WithMessage($"Email cannot be longer than {MaxEmailLength} characters")
                .EmailAddress().WithMessage("Email is not a valid email address");

            RuleFor(r => r.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(MinPasswordLength).WithMessage($"Password must be at least {MinPasswordLength} characters")
                .MaximumLength(MaxPasswordLength).WithMessage($"Password cannot be longer than {MaxPasswordLength} characters");

            RuleFor(r => r.FirstName)
                .NotEmpty().WithMessage("First name is required")
                .MaximumLength(MaxNameLength).WithMessage($"First name cannot be longer than {MaxNameLength} characters");

            RuleFor(r => r.LastName)
                .NotEmpty().WithMessage("Last name is required")
                .MaximumLength(MaxNameLength).WithMessage($"Last name cannot be longer than {MaxNameLength} characters");

            RuleFor(r => r.DateOfBirth)
                .NotNull().WithMessage("Date of birth is required")
                .Must(dob => dob == null || (dob >= EarliestDateOfBirth
                    && dob < DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime)))
                .WithMessage("Date of birth must be in the past");

            RuleFor(r => r.Gender)
                .MaximumLength(MaxGenderLength).WithMessage($"Gender cannot be longer than {MaxGenderLength} characters");
        }
    }
}
