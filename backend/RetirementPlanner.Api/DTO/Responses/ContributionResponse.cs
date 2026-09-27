namespace RetirementPlanner.DTO.Responses
{
    public record ContributionResponse(int Id, int GoalId, int Year, int Month, decimal Amount, DateTime RecordedAt);
}
