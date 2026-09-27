using RetirementPlanner.DTO.Requests;
using RetirementPlanner.Mapping;
using RetirementPlanner.Models;

namespace RetirementPlanner.Tests.Mapping
{
    public class ContributionMappingsTests
    {
        [Fact]
        public void AddInvestmentRequest_ToCommand_MapsEveryField()
        {
            var command = new AddInvestmentRequest { GoalId = 4, Year = 2026, Month = 9, MonthlyInvestment = 2_500m }.ToCommand();

            Assert.Equal(new RecordContributionCommand(GoalId: 4, Year: 2026, Month: 9, Amount: 2_500m), command);
        }
    }
}
