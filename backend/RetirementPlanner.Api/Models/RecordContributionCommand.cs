namespace RetirementPlanner.Models
{
    /// <summary>Record one month's contribution to a goal owned by UserId.</summary>
    public record RecordContributionCommand(int UserId, int GoalId, int Year, int Month, decimal Amount);
}
