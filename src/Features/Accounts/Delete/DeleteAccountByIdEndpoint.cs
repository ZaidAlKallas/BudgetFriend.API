namespace BudgetFriend.API.Features.Accounts.Delete;

public static class DeleteAccountByIdEndpoint
{
    public static void MapDeleteAccount(this IEndpointRouteBuilder app) =>
        app.MapDelete("/{accountId}", HandleAsync)
            .WithName("Delete Account by Id")
            .WithSummary("Delete an account by its ID")
            .WithDescription("Deletes an account associated with the specified ID")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    public static async Task<IResult> HandleAsync(
        Guid accountId,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        var accountExists = await dbContext.Accounts
            .AnyAsync(a => a.Id == accountId && a.UserId == currentUser.UserId, cancellationToken);

        if (!accountExists)
            return Results.NotFound();

        var hasTransactions = await dbContext.Transactions
            .AnyAsync(t => t.AccountId == accountId, cancellationToken);

        var hasTransfers = await dbContext.Transfers
            .AnyAsync(t => t.FromAccountId == accountId || t.ToAccountId == accountId, cancellationToken);

        if (hasTransactions || hasTransfers)
            return Results.Conflict(new { message = "Account cannot be deleted because it has associated transactions or transfers." });

        await dbContext.Accounts
            .Where(a => a.Id == accountId)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogInformation("Account {AccountId} deleted by user {UserId}", accountId, currentUser.UserId);
        return Results.NoContent();
    }
}
