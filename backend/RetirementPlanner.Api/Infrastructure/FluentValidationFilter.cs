using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RetirementPlanner.Infrastructure
{
    /// <summary>
    /// Validates every action argument that has a registered <see cref="IValidator{T}"/> before the action
    /// runs. Failures become a 400 ValidationProblemDetails with one entry per field, keyed by the request's
    /// camelCase JSON field name. Model-binding errors (for example malformed JSON) are rejected earlier by
    /// [ApiController] in the same format.
    /// </summary>
    public class FluentValidationFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _services;

        public FluentValidationFilter(IServiceProvider services)
        {
            _services = services;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var errors = new List<(string PropertyName, string Message)>();

            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null)
                    continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (_services.GetService(validatorType) is not IValidator validator)
                    continue;

                var result = await validator.ValidateAsync(
                    new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
                errors.AddRange(result.Errors.Select(e => (e.PropertyName, e.ErrorMessage)));
            }

            if (errors.Count > 0)
            {
                context.Result = FieldErrorResults.ValidationProblem(context.HttpContext, errors);
                return;
            }

            await next();
        }
    }
}
