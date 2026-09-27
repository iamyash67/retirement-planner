using RetirementPlanner.Models;

namespace RetirementPlanner.Services.Interfaces
{
    public interface IUserService
    {
        Task<Profile?> AuthenticateAsync(string username, string password);
    }
}