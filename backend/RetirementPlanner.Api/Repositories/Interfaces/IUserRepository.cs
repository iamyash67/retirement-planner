using RetirementPlanner.Models;

namespace RetirementPlanner.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<Profile?> ValidateCredentialsAsync(string username, string password);
    }
}