using System.Text.Json;
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
        public void AuthenticatedUser_ToLoginResponse_MapsEveryField()
        {
            var response = User.ToLoginResponse();

            Assert.Equal(7, response.ProfileId);
            Assert.Equal("Jane", response.FirstName);
            Assert.Equal("Doe", response.LastName);
            Assert.Equal(36, response.Age);
            Assert.Null(response.Gender);
            Assert.Equal("jane@example.com", response.UserName);
        }

        [Fact]
        public void LoginResponse_SerializesToTheShapeTheFrontendExpects()
        {
            var json = JsonSerializer.Serialize(User.ToLoginResponse(), JsonSerializerOptions.Web);
            var keys = JsonDocument.Parse(json).RootElement.EnumerateObject().Select(p => p.Name).ToArray();

            Assert.Equal(["profileId", "firstName", "lastName", "age", "gender", "userName"], keys);
        }
    }
}
