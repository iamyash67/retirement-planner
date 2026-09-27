namespace RetirementPlanner.DTO.Requests
{
    public class LoginRequest
    {
        /// <summary>The user's email address. Named UserName so existing clients keep working.</summary>
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
