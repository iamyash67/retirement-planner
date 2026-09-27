namespace RetirementPlanner.DTO.Responses
{
    /// <summary>A goal as the API returns it. CurrentSavings includes every recorded investment.</summary>
    public record GoalResponse(
        int ProfileId,
        int GoalId,
        int CurrentAge,
        int RetirementAge,
        decimal TargetSavings,
        decimal MonthlyContribution,
        decimal CurrentSavings);
}
