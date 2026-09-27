namespace RetirementPlanner.DTO.Requests
{
    /// <summary>Body of POST api/goals. The owner is always the current user, taken from the access token.</summary>
    public class CreateGoalRequest
    {
        public int CurrentAge { get; set; }
        public int RetirementAge { get; set; }
        public decimal TargetSavings { get; set; }
        public decimal CurrentSavings { get; set; }

        // Optional simulation inputs; defaults are used when omitted. Rates are fractions (0.06 = 6 %).
        public string? Name { get; set; }
        public decimal? ExpectedAnnualReturn { get; set; }
        public decimal? ReturnVolatility { get; set; }
        public decimal? InflationRate { get; set; }
        public decimal? AnnualContributionIncrease { get; set; }
    }
}
