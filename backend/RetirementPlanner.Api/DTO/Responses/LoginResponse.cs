namespace RetirementPlanner.DTO.Responses
{
    /// <summary>The logged-in user's profile. ProfileId is the user id and UserName is the email.</summary>
    public record LoginResponse(
        int ProfileId,
        string FirstName,
        string LastName,
        int Age,
        string? Gender,
        string UserName);
}
