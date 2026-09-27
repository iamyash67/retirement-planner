using RetirementPlanner.DTO.Requests;
using RetirementPlanner.DTO.Responses;
using RetirementPlanner.Models;

namespace RetirementPlanner.Mapping
{
    public static class ContributionMappings
    {
        public static RecordContributionCommand ToCommand(this AddContributionRequest request, int userId, int goalId) => new(
            UserId: userId,
            GoalId: goalId,
            Year: request.Year,
            Month: request.Month,
            Amount: request.Amount);

        public static ContributionResponse ToResponse(this Contribution contribution) => new(
            Id: contribution.Id,
            GoalId: contribution.GoalId,
            Year: contribution.Year,
            Month: contribution.Month,
            Amount: contribution.Amount,
            RecordedAt: contribution.RecordedAt);
    }
}
