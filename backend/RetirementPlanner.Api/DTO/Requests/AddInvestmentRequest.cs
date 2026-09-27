namespace RetirementPlanner.DTO.Requests
{
    public class AddInvestmentRequest
    {
        public int GoalId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal MonthlyInvestment { get; set; }
    }
}
