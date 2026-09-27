using RetirementPlanner.Models;

namespace RetirementPlanner.Services.Interfaces
{
    public interface IUserService
    {
        /// <summary>Returns the user when the email and password match, otherwise null.</summary>
        Task<AuthenticatedUser?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);

        /// <summary>The user and their profile, or null if either doesn't exist.</summary>
        Task<AuthenticatedUser?> GetAsync(int userId, CancellationToken cancellationToken = default);
    }
}
