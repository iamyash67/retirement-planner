namespace RetirementPlanner.DTO.Responses
{
    /// <summary>A goal as the API returns it. CurrentSavings includes every recorded contribution.</summary>
    public record GoalResponse(
        int Id,
        string Name,
        int CurrentAge,
        int RetirementAge,
        decimal TargetSavings,
        decimal MonthlyContribution,
        decimal CurrentSavings);
}
