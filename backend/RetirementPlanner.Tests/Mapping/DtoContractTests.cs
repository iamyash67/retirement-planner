using FluentValidation;
using RetirementPlanner.DTO.Responses;
using RetirementPlanner.Validators;

namespace RetirementPlanner.Tests.Mapping
{
    /// <summary>Guards the rules for request and response DTOs across the whole API assembly.</summary>
    public class DtoContractTests
    {
        private static readonly Type[] ApiTypes = typeof(LoginRequestValidator).Assembly.GetTypes();

        public static TheoryData<Type> RequestTypes() =>
            new(ApiTypes.Where(t => t.Namespace == "RetirementPlanner.DTO.Requests" && t.IsClass));

        public static TheoryData<Type> ResponseTypes() =>
            new(ApiTypes.Where(t => t.Namespace == "RetirementPlanner.DTO.Responses" && t.IsClass));

        [Theory]
        [MemberData(nameof(RequestTypes))]
        public void EveryRequestDto_HasAValidator(Type requestType)
        {
            var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);

            Assert.Contains(ApiTypes, t => !t.IsAbstract && validatorInterface.IsAssignableFrom(t));
        }

        [Theory]
        [MemberData(nameof(ResponseTypes))]
        public void NoResponseDto_ExposesPasswordOrRefreshTokenFields(Type responseType)
        {
            Assert.DoesNotContain(responseType.GetProperties(),
                p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase)
                  || p.Name.Contains("Hash", StringComparison.OrdinalIgnoreCase)
                  || p.Name.Contains("RefreshToken", StringComparison.OrdinalIgnoreCase));
        }

        [Theory]
        [MemberData(nameof(ResponseTypes))]
        public void NoResponseDto_ContainsDomainModels(Type responseType)
        {
            Assert.DoesNotContain(responseType.GetProperties(),
                p => p.PropertyType.Namespace == "RetirementPlanner.Models");
        }

        [Fact]
        public void TheContractTestsFindTheDtos()
        {
            Assert.Equal(5, RequestTypes().Count);
            Assert.Equal(5, ResponseTypes().Count);
            Assert.Contains(ApiTypes, t => t == typeof(AuthResponse));
        }
    }
}
