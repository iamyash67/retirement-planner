namespace RetirementPlanner.Models
{
    public class FinancialYearData
    {
        public int Id { get; set; }
        public int GoalId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal MonthlyInvestment { get; set; }
        public bool IsInvested { get; set; }
    }
}