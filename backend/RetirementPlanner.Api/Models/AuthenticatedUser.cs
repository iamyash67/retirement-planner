namespace RetirementPlanner.Models
{
    /// <summary>A user whose credentials have been verified, with their profile details.</summary>
    public class AuthenticatedUser
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public int Age { get; set; }
        public string? Gender { get; set; }
    }
}
