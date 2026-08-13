using BudgetFriend.API.Features.Transfers.Create;
using BudgetFriend.API.Features.Transfers.Delete;
using BudgetFriend.API.Features.Transfers.GetAll;
using BudgetFriend.API.Features.Transfers.GetById;

namespace BudgetFriend.API.Features.Transfers;

public static class TransferEndpoints
{
    public static WebApplication MapTransferEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/transfers")
            .RequireAuthorization()
            .WithTags("Transfers")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapCreateTransfer();
        group.MapGetAllTransfers();
        group.MapGetTransferById();
        group.MapDeleteTransfer();

        return app;
    }
}
