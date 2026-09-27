namespace RetirementPlanner.DTO.Responses
{
    public record UserResponse(
        int Id,
        string Email,
        string FirstName,
        string LastName,
        int Age,
        string? Gender);
}
