namespace RetirementPlanner.DTO.Responses
{
    /// <summary>Progress towards a goal, formatted as a percentage such as "10.25%".</summary>
    public record ProgressResponse(int GoalId, string Progress);
}
