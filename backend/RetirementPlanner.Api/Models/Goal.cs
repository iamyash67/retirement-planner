namespace RetirementPlanner.Models
{
    /// <summary>
    /// A retirement goal. CurrentSavings is the savings entered at creation plus every recorded contribution.
    /// </summary>
    public class Goal
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CurrentAge { get; set; }
        public int RetirementAge { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal CurrentSavings { get; set; }
        public decimal ExpectedAnnualReturn { get; set; }
        public decimal ReturnVolatility { get; set; }
        public decimal InflationRate { get; set; }
        public decimal AnnualContributionIncrease { get; set; }
        public decimal PlannedMonthlyContribution { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
