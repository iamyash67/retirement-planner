namespace RetirementPlanner.DTO
{
    public class GoalDTO
    {
        public int ProfileId { get; set; }
        public int CurrentAge { get; set; }
        public int RetirementAge { get; set; }
        public decimal TargetSavings { get; set; }
        public decimal CurrentSavings { get; set; }

        // Optional; GoalService fills in defaults when these are omitted.
        public string? Name { get; set; }
        public decimal? ExpectedAnnualReturn { get; set; }
        public decimal? ReturnVolatility { get; set; }
        public decimal? InflationRate { get; set; }
        public decimal? AnnualContributionIncrease { get; set; }
    }
}
