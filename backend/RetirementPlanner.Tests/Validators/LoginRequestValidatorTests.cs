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
            _validator.TestValidate(new LoginRequest { UserName = "demo@example.com", Password = "demo123" })
                .ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void MissingUserName_Fails(string? userName)
        {
            _validator.TestValidate(new LoginRequest { UserName = userName!, Password = "demo123" })
                .ShouldHaveValidationErrorFor(r => r.UserName).WithErrorMessage("Username is required")
                .Only();
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void MissingPassword_Fails(string? password)
        {
            _validator.TestValidate(new LoginRequest { UserName = "demo@example.com", Password = password! })
                .ShouldHaveValidationErrorFor(r => r.Password).WithErrorMessage("Password is required")
                .Only();
        }
    }
}
