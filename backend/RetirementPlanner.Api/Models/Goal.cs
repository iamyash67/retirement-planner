namespace RetirementPlanner.Models
{
    /// <summary>
    /// The goal returned by the API. ProfileId is the owning user's id, and CurrentSavings is the
    /// savings entered at creation plus every recorded contribution.
    /// </summary>
    public class Goal
    {
        public int ProfileId { get; set; }
        public int GoalId { get; set; }
        public int CurrentAge { get; set; }
        public int RetirementAge { get; set; }
        public decimal TargetSavings { get; set; }
        public decimal MonthlyContribution { get; set; }
        public decimal CurrentSavings { get; set; }
    }
}
