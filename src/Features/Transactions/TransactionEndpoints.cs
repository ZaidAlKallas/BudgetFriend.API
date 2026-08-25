using BudgetFriend.API.Features.Transactions.Create;
using BudgetFriend.API.Features.Transactions.Delete;
using BudgetFriend.API.Features.Transactions.GetAll;
using BudgetFriend.API.Features.Transactions.GetById;
using BudgetFriend.API.Features.Transactions.Update;

namespace BudgetFriend.API.Features.Transactions;

public static class TransactionEndpoints
{
    public static WebApplication MapTransactionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/transactions")
            .RequireAuthorization()
            .WithTags("Transactions")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapCreateTransaction();
        group.MapGetAllTransactions();
        group.MapGetTransactionById();
        group.MapUpdateTransaction();
        group.MapDeleteTransaction();

        return app;
    }
}
