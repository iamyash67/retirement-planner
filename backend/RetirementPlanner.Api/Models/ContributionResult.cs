namespace RetirementPlanner.Models
{
    public enum ContributionStatus
    {
        Recorded,
        GoalNotFound,
        ExceedsTarget,
        AlreadyRecorded
    }

    /// <summary>The outcome of recording a contribution. Contribution is set when Status is Recorded.</summary>
    public record ContributionResult(ContributionStatus Status, Contribution? Contribution = null);
}
