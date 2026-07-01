using BudgetFriend.API.Database;
using BudgetFriend.API.Features.Authentication;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Transactions.GetAll;

public static class GetAllTransactionsEndpoint
{
    public static void MapGetAllTransactions(this IEndpointRouteBuilder app) =>
        app.MapGet("/", HandleAsync)
            .WithName("Get Transactions")
            .WithSummary("Get all transactions for the current user")
            .WithDescription("Retrieves all transactions across all accounts for the current user")
            .Produces<List<GetTransactionResponse>>(StatusCodes.Status200OK);

    public static async Task<IResult> HandleAsync(
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var transactions = await dbContext.Transactions
            .Where(t => t.Account.UserId == currentUser.UserId)
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Select(t => new GetTransactionResponse(
                t.Id,
                t.AccountId,
                t.Account.Name,
                t.CategoryId,
                t.Category.Name,
                t.Account.Currency,
                t.Amount,
                t.Note,
                t.TransactionDate,
                t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Results.Ok(transactions);
    }
}
