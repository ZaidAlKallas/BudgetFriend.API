using BudgetFriend.API.Database;
using BudgetFriend.API.Features.Authentication;
using BudgetFriend.API.Features.Transactions.GetAll;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Transactions.GetById;

public static class GetTransactionByIdEndpoint
{
    public static void MapGetTransactionById(this IEndpointRouteBuilder app) =>
        app.MapGet("/{transactionId}", HandleAsync)
            .WithName("Get Transaction by Id")
            .WithSummary("Get a transaction by its ID")
            .WithDescription("Retrieves a transaction associated with the specified ID")
            .Produces<GetTransactionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

    public static async Task<IResult> HandleAsync(
        Guid transactionId,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Transactions
            .Where(t => t.Account.UserId == currentUser.UserId && t.Id == transactionId)
            .Select(t => new GetTransactionResponse(
                t.Id,
                t.AccountId,
                t.Account.Name,
                t.CategoryId,
                t.Category.Name,
                t.Amount,
                t.Note,
                t.TransactionDate,
                t.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        return transaction is not null ? Results.Ok(transaction) : Results.NotFound();
    }
}
