using RetirementPlanner.DTO.Requests;
using RetirementPlanner.Models;

namespace RetirementPlanner.Mapping
{
    public static class ContributionMappings
    {
        public static RecordContributionCommand ToCommand(this AddInvestmentRequest request) => new(
            GoalId: request.GoalId,
            Year: request.Year,
            Month: request.Month,
            Amount: request.MonthlyInvestment);
    }
}
