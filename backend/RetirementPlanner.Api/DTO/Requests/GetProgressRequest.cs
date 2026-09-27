namespace RetirementPlanner.DTO.Requests
{
    /// <summary>Bound from the route: GET api/financial/progress/{goalId}.</summary>
    public class GetProgressRequest
    {
        public int GoalId { get; set; }
    }
}
