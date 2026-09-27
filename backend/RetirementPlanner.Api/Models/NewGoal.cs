namespace RetirementPlanner.Models
{
    /// <summary>Everything needed to insert a row into the Goals table.</summary>
    public class NewGoal
    {
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
    }
}
