using System.Text.Json;
using RetirementPlanner.Auth;
using RetirementPlanner.DTO.Requests;
using RetirementPlanner.Mapping;
using RetirementPlanner.Models;

namespace RetirementPlanner.Tests.Mapping
{
    public class UserMappingsTests
    {
        private static readonly AuthenticatedUser User = new()
        {
            UserId = 7,
            Email = "jane@example.com",
            FirstName = "Jane",
            LastName = "Doe",
            DateOfBirth = new DateOnly(1990, 6, 15),
            Age = 36,
            Gender = null
        };

        [Fact]
        public void AuthenticatedUser_ToResponse_MapsEveryField()
        {
            var response = User.ToResponse();

            Assert.Equal(7, response.Id);
            Assert.Equal("jane@example.com", response.Email);
            Assert.Equal("Jane", response.FirstName);
            Assert.Equal("Doe", response.LastName);
            Assert.Equal(36, response.Age);
            Assert.Null(response.Gender);
        }

        [Fact]
        public void AuthSession_ToResponse_OmitsTheRefreshToken()
        {
            var expiresAt = new DateTimeOffset(2026, 9, 27, 10, 15, 0, TimeSpan.Zero);
            var session = new AuthSession(User, new AccessToken("access.jwt", expiresAt), "raw-refresh-token", expiresAt.AddDays(7));

            var response = session.ToResponse();
            var json = JsonSerializer.Serialize(response, JsonSerializerOptions.Web);

            Assert.Equal("access.jwt", response.AccessToken);
            Assert.Equal(expiresAt, response.ExpiresAt);
            Assert.Equal(7, response.User.Id);
            Assert.DoesNotContain("raw-refresh-token", json);
            Assert.Equal(["accessToken", "expiresAt", "user"],
                JsonDocument.Parse(json).RootElement.EnumerateObject().Select(p => p.Name).ToArray());
        }

        [Fact]
        public void RegisterRequest_ToCommand_TrimsAndNormalisesOptionalGender()
        {
            var command = new RegisterRequest
            {
                Email = " jane@example.com ",
                Password = " keep spaces ",
                FirstName = " Jane ",
                LastName = " Doe ",
                DateOfBirth = new DateOnly(1990, 6, 15),
                Gender = "  "
            }.ToCommand();

            Assert.Equal(new RegisterCommand("jane@example.com", " keep spaces ", "Jane", "Doe", new DateOnly(1990, 6, 15), null), command);
        }
    }
}
