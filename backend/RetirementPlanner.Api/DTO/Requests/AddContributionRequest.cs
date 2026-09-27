namespace RetirementPlanner.DTO.Requests
{
    /// <summary>Body of POST api/goals/{id}/contributions. The goal comes from the route, the user from the token.</summary>
    public class AddContributionRequest
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Amount { get; set; }
    }
}
