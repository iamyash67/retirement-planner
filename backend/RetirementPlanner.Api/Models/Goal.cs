namespace RetirementPlanner.Models
{
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
