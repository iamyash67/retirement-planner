namespace RetirementPlanner.Models
{
    public record RegisterCommand(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        DateOnly DateOfBirth,
        string? Gender);
}
