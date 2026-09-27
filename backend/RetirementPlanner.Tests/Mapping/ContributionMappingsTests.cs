using RetirementPlanner.DTO.Requests;
using RetirementPlanner.Mapping;
using RetirementPlanner.Models;

namespace RetirementPlanner.Tests.Mapping
{
    public class ContributionMappingsTests
    {
        [Fact]
        public void AddContributionRequest_ToCommand_TakesUserAndGoalFromTheCaller()
        {
            var command = new AddContributionRequest { Year = 2026, Month = 9, Amount = 2_500m }.ToCommand(userId: 3, goalId: 4);

            Assert.Equal(new RecordContributionCommand(UserId: 3, GoalId: 4, Year: 2026, Month: 9, Amount: 2_500m), command);
        }

        [Fact]
        public void Contribution_ToResponse_MapsEveryField()
        {
            var recordedAt = new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc);
            var response = new Contribution { Id = 7, GoalId = 4, Year = 2026, Month = 9, Amount = 2_500m, RecordedAt = recordedAt }
                .ToResponse();

            Assert.Equal(7, response.Id);
            Assert.Equal(4, response.GoalId);
            Assert.Equal(2026, response.Year);
            Assert.Equal(9, response.Month);
            Assert.Equal(2_500m, response.Amount);
            Assert.Equal(recordedAt, response.RecordedAt);
        }
    }
}
