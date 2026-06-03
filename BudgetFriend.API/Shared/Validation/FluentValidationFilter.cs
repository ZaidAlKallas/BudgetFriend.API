using FluentValidation;

namespace BudgetFriend.API.Shared.Validation;

public sealed class FluentValidationFilter<TRequest>(IValidator<TRequest> validator)
    : IEndpointFilter {
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next) {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null) {
            return await next(context);
        }

        var validationResult = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
        if (validationResult.IsValid) {
            return await next(context);
        }

        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }
}
