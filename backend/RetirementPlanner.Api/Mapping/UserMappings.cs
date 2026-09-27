using RetirementPlanner.DTO.Requests;
using RetirementPlanner.DTO.Responses;
using RetirementPlanner.Models;

namespace RetirementPlanner.Mapping
{
    public static class UserMappings
    {
        /// <summary>Only profile fields; credentials never leave the service layer.</summary>
        public static UserResponse ToResponse(this AuthenticatedUser user) => new(
            Id: user.UserId,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Age: user.Age,
            Gender: user.Gender);

        /// <summary>The refresh token is left out on purpose: it is sent only as an httpOnly cookie.</summary>
        public static AuthResponse ToResponse(this AuthSession session) => new(
            AccessToken: session.AccessToken.Token,
            ExpiresAt: session.AccessToken.ExpiresAt,
            User: session.User.ToResponse());

        /// <summary>Validation guarantees DateOfBirth is present before this runs.</summary>
        public static RegisterCommand ToCommand(this RegisterRequest request) => new(
            Email: request.Email.Trim(),
            Password: request.Password,
            FirstName: request.FirstName.Trim(),
            LastName: request.LastName.Trim(),
            DateOfBirth: request.DateOfBirth ?? throw new InvalidOperationException("DateOfBirth was not validated."),
            Gender: string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim());
    }
}
