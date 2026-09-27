namespace RetirementPlanner.Models
{
    public enum GoalCreationStatus
    {
        Created,
        UserNotFound,
        AlreadyExists
    }

    /// <summary>The outcome of creating a goal. Goal is the new goal when Status is Created.</summary>
    public record CreateGoalResult(GoalCreationStatus Status, Goal? Goal = null);
}
