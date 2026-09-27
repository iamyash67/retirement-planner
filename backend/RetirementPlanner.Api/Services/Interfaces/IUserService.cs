using RetirementPlanner.Models;

namespace RetirementPlanner.Services.Interfaces
{
    public interface IUserService
    {
        /// <summary>Returns the user's profile when the email and password match, otherwise null.</summary>
        Task<Profile?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
    }
}
