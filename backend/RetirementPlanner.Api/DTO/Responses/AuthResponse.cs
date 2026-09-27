namespace RetirementPlanner.DTO.Responses
{
    /// <summary>
    /// Returned by register, login and refresh. The refresh token is never in the body: it travels only in
    /// an httpOnly cookie, so page scripts can't read it.
    /// </summary>
    public record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserResponse User);
}
