namespace RetirementPlanner.Models
{
    public record RecordContributionCommand(int GoalId, int Year, int Month, decimal Amount);
}
