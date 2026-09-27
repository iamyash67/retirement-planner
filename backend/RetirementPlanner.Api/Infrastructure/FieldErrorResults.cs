using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RetirementPlanner.Infrastructure
{
    public static class FieldErrorResults
    {
        /// <summary>
        /// A 400 ValidationProblemDetails whose errors are keyed by camelCase field name ("retirementAge").
        /// It starts from an empty ModelStateDictionary: that dictionary's keys are case-insensitive, so adding
        /// "profileId" to one that model binding already filled would land under the existing "ProfileId" key.
        /// </summary>
        public static IActionResult ValidationProblem(
            HttpContext httpContext, IEnumerable<(string PropertyName, string Message)> errors)
        {
            var modelState = new ModelStateDictionary();
            foreach (var (propertyName, message) in errors)
                modelState.AddModelError(propertyName.ToFieldKey(), message);

            var problemDetails = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
                .CreateValidationProblemDetails(httpContext, modelState, StatusCodes.Status400BadRequest);

            var result = new BadRequestObjectResult(problemDetails);
            result.ContentTypes.Add("application/problem+json");
            return result;
        }
    }
}
