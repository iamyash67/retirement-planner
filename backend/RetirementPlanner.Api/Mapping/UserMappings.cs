using RetirementPlanner.DTO.Responses;
using RetirementPlanner.Models;

namespace RetirementPlanner.Mapping
{
    public static class UserMappings
    {
        /// <summary>Maps only profile fields; credentials never leave the service layer.</summary>
        public static LoginResponse ToLoginResponse(this AuthenticatedUser user) => new(
            ProfileId: user.UserId,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Age: user.Age,
            Gender: user.Gender,
            UserName: user.Email);
    }
}
