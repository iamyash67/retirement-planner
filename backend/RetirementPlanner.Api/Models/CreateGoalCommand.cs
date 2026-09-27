namespace RetirementPlanner.Models
{
    /// <summary>Input for creating a goal. Null simulation inputs are replaced by defaults.</summary>
    public record CreateGoalCommand(
        int UserId,
        int CurrentAge,
        int RetirementAge,
        decimal TargetAmount,
        decimal CurrentSavings,
        string? Name = null,
        decimal? ExpectedAnnualReturn = null,
        decimal? ReturnVolatility = null,
        decimal? InflationRate = null,
        decimal? AnnualContributionIncrease = null);
}
