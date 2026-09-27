namespace RetirementPlanner.Models
{
    /// <summary>A row of the Profiles table (1:1 with <see cref="User"/>).</summary>
    public class UserProfile
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string? Gender { get; set; }
    }
}
