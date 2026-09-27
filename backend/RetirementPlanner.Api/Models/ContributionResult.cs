namespace RetirementPlanner.Models
{
    public enum ContributionStatus
    {
        Recorded,
        GoalNotFound,
        ExceedsTarget,
        AlreadyRecorded
    }

    /// <summary>The outcome of recording a contribution. Goal is the updated goal when Status is Recorded.</summary>
    public record ContributionResult(ContributionStatus Status, Goal? Goal = null);
}
