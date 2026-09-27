namespace RetirementPlanner.Models
{
    public class Login
    {
        /// <summary>The user's email address. The property keeps its name so existing clients still work.</summary>
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
