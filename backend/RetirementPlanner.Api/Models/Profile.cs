namespace RetirementPlanner.Models
{
    /// <summary>
    /// The profile returned by the login endpoint. ProfileId is the user's id and UserName is the email.
    /// </summary>
    public class Profile
    {
        public int ProfileId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int Age { get; set; }
        public string? Gender { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}
