namespace BudgetFriend.API.Shared.Validation;

public static class ValidationEndpointExtensions {
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
        where TRequest : class => builder
            .AddEndpointFilter<FluentValidationFilter<TRequest>>()
            .ProducesValidationProblem();
}