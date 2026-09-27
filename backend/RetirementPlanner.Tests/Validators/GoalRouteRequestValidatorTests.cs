using FluentValidation.TestHelper;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.Validators;

namespace RetirementPlanner.Tests.Validators
{
    public class GoalRouteRequestValidatorTests
    {
        private readonly GoalRouteRequestValidator _validator = new();

        [Fact]
        public void PositiveId_Passes()
        {
            _validator.TestValidate(new GoalRouteRequest { Id = 1 }).ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void NonPositiveId_Fails(int id)
        {
            _validator.TestValidate(new GoalRouteRequest { Id = id })
                .ShouldHaveValidationErrorFor(r => r.Id).WithErrorMessage("Invalid Goal ID");
        }
    }
}
