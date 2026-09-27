using System.Text.Json;

namespace RetirementPlanner.Infrastructure
{
    public static class FieldKeyExtensions
    {
        /// <summary>
        /// Turns a C# property path into the key used in ValidationProblemDetails.errors, matching the request's
        /// camelCase JSON field names: "RetirementAge" becomes "retirementAge".
        /// </summary>
        public static string ToFieldKey(this string propertyName) =>
            string.Join('.', propertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
    }
}
