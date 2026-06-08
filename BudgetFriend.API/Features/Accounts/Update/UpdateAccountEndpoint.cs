using BudgetFriend.API.Database;
using BudgetFriend.API.Features.Authentication;
using BudgetFriend.API.Shared.Validation;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Accounts.Update;

public static class UpdateAccountEndpoint
{
    public static void MapUpdateAccount(this IEndpointRouteBuilder app) =>
        app.MapPut("/{accountId}", HandleAsync)
            .WithValidation<UpdateAccountRequest>()
            .WithName("Update Account")
            .WithSummary("Update an account by its ID")
            .WithDescription("Updates an account by its ID")
            .Produces<UpdateAccountResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    public static async Task<IResult> HandleAsync(
        Guid accountId,
        UpdateAccountRequest request,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Accounts
                .AnyAsync(a => a.Name == request.Name && a.UserId == currentUser.UserId && a.Id != accountId, cancellationToken);

        if (exists)
            return Results.Conflict($"An account with the name '{request.Name}' already exists.");

        var totalUpdated = await dbContext.Accounts
            .Where(a => a.UserId == currentUser.UserId && a.Id == accountId)
            .ExecuteUpdateAsync(account => account
                .SetProperty(a => a.Name, request.Name)
                .SetProperty(a => a.InitialBalance, request.InitialBalance),
                cancellationToken);
        if (totalUpdated == 0)
            return Results.NotFound();

        logger.LogInformation("Account {AccountId} updated by user {UserId}", accountId, currentUser.UserId);
        return Results.Ok(new UpdateAccountResponse(
                accountId,
                request.Name,
                request.InitialBalance
        ));
    }
}
