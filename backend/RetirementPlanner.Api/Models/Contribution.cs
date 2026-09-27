namespace RetirementPlanner.Models
{
    public class Contribution
    {
        public int Id { get; set; }
        public int GoalId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Amount { get; set; }
        public DateTime RecordedAt { get; set; }
    }
}
