using FluentValidation.TestHelper;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.Validators;

namespace RetirementPlanner.Tests.Validators
{
    public class RouteRequestValidatorTests
    {
        [Fact]
        public void GetGoal_PositiveProfileId_Passes()
        {
            new GetGoalRequestValidator().TestValidate(new GetGoalRequest { ProfileId = 1 })
                .ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void GetGoal_NonPositiveProfileId_Fails(int profileId)
        {
            new GetGoalRequestValidator().TestValidate(new GetGoalRequest { ProfileId = profileId })
                .ShouldHaveValidationErrorFor(r => r.ProfileId).WithErrorMessage("Invalid Profile ID");
        }

        [Fact]
        public void GetProgress_PositiveGoalId_Passes()
        {
            new GetProgressRequestValidator().TestValidate(new GetProgressRequest { GoalId = 1 })
                .ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void GetProgress_NonPositiveGoalId_Fails(int goalId)
        {
            new GetProgressRequestValidator().TestValidate(new GetProgressRequest { GoalId = goalId })
                .ShouldHaveValidationErrorFor(r => r.GoalId).WithErrorMessage("Invalid Goal ID");
        }
    }
}
