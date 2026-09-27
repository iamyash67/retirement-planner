using FluentValidation.TestHelper;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.Tests.TestSupport;
using RetirementPlanner.Validators;

namespace RetirementPlanner.Tests.Validators
{
    public class RegisterRequestValidatorTests
    {
        private static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);
        private readonly RegisterRequestValidator _validator = new(new FixedTimeProvider(Now));

        private static RegisterRequest Valid() => new()
        {
            Email = "jane@example.com",
            Password = "correct-horse",
            FirstName = "Jane",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1990, 6, 15),
            Gender = null
        };

        [Fact]
        public void ValidRequest_Passes()
        {
            _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("", "Email is required")]
        [InlineData("not-an-email", "Email is not a valid email address")]
        public void InvalidEmail_Fails(string email, string message)
        {
            var request = Valid();
            request.Email = email;

            _validator.TestValidate(request).ShouldHaveValidationErrorFor(r => r.Email).WithErrorMessage(message);
        }

        [Fact]
        public void EmailTooLong_Fails()
        {
            var request = Valid();
            request.Email = new string('a', 250) + "@example.com";

            _validator.TestValidate(request).ShouldHaveValidationErrorFor(r => r.Email)
                .WithErrorMessage("Email cannot be longer than 255 characters");
        }

        [Theory]
        [InlineData("", "Password is required")]
        [InlineData("short12", "Password must be at least 8 characters")]
        public void WeakPassword_Fails(string password, string message)
        {
            var request = Valid();
            request.Password = password;

            _validator.TestValidate(request).ShouldHaveValidationErrorFor(r => r.Password).WithErrorMessage(message);
        }

        [Fact]
        public void PasswordTooLong_Fails()
        {
            var request = Valid();
            request.Password = new string('p', RegisterRequestValidator.MaxPasswordLength + 1);

            _validator.TestValidate(request).ShouldHaveValidationErrorFor(r => r.Password)
                .WithErrorMessage("Password cannot be longer than 128 characters").Only();
        }

        [Fact]
        public void MissingNames_Fail()
        {
            var request = Valid();
            request.FirstName = " ";
            request.LastName = "";

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(r => r.FirstName).WithErrorMessage("First name is required");
            result.ShouldHaveValidationErrorFor(r => r.LastName).WithErrorMessage("Last name is required");
        }

        [Fact]
        public void NamesTooLong_Fail()
        {
            var request = Valid();
            request.FirstName = new string('f', 101);
            request.LastName = new string('l', 101);

            var result = _validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(r => r.FirstName).WithErrorMessage("First name cannot be longer than 100 characters");
            result.ShouldHaveValidationErrorFor(r => r.LastName).WithErrorMessage("Last name cannot be longer than 100 characters");
        }

        [Fact]
        public void MissingDateOfBirth_Fails()
        {
            var request = Valid();
            request.DateOfBirth = null;

            _validator.TestValidate(request).ShouldHaveValidationErrorFor(r => r.DateOfBirth)
                .WithErrorMessage("Date of birth is required").Only();
        }

        [Theory]
        [InlineData(2026, 9, 27)] // today (per the injected TimeProvider)
        [InlineData(2030, 1, 1)]
        [InlineData(1899, 12, 31)]
        public void DateOfBirthNotInThePast_Fails(int year, int month, int day)
        {
            var request = Valid();
            request.DateOfBirth = new DateOnly(year, month, day);

            _validator.TestValidate(request).ShouldHaveValidationErrorFor(r => r.DateOfBirth)
                .WithErrorMessage("Date of birth must be in the past").Only();
        }

        [Fact]
        public void GenderTooLong_Fails_AndAnyShorterTextIsAccepted()
        {
            var request = Valid();
            request.Gender = "Non-binary";
            _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();

            request.Gender = new string('g', 31);
            _validator.TestValidate(request).ShouldHaveValidationErrorFor(r => r.Gender)
                .WithErrorMessage("Gender cannot be longer than 30 characters");
        }
    }
}
