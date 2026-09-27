using FluentValidation.TestHelper;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.Validators;

namespace RetirementPlanner.Tests.Validators
{
    public class LoginRequestValidatorTests
    {
        private readonly LoginRequestValidator _validator = new();

        [Fact]
        public void ValidRequest_Passes()
        {
            _validator.TestValidate(new LoginRequest { Email = "demo@example.com", Password = "demo123" })
                .ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ShortPassword_IsNotRejectedAtLogin()
        {
            // Length rules apply when registering; existing accounts (like the demo user) must still sign in.
            _validator.TestValidate(new LoginRequest { Email = "demo@example.com", Password = "x" })
                .ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void MissingEmail_Fails(string? email)
        {
            _validator.TestValidate(new LoginRequest { Email = email!, Password = "demo123" })
                .ShouldHaveValidationErrorFor(r => r.Email).WithErrorMessage("Email is required")
                .Only();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void MissingPassword_Fails(string? password)
        {
            _validator.TestValidate(new LoginRequest { Email = "demo@example.com", Password = password! })
                .ShouldHaveValidationErrorFor(r => r.Password).WithErrorMessage("Password is required")
                .Only();
        }
    }
}
