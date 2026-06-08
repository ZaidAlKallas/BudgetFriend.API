using BudgetFriend.API.Database;
using BudgetFriend.API.Features.Authentication;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Transactions.Delete;

public static class DeleteTransactionByIdEndpoint
{
    public static void MapDeleteTransaction(this IEndpointRouteBuilder app) =>
        app.MapDelete("/{transactionId}", HandleAsync)
            .WithName("Delete Transaction by Id")
            .WithSummary("Delete a transaction by its ID")
            .WithDescription("Deletes a transaction associated with the specified ID")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

    public static async Task<IResult> HandleAsync(
        Guid transactionId,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var deletedRows = await dbContext.Transactions
            .Where(t => t.Account.UserId == currentUser.UserId && t.Id == transactionId)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedRows == 0 ? Results.NotFound() : Results.NoContent();
    }
}
