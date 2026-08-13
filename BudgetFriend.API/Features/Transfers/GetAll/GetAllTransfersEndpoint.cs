namespace BudgetFriend.API.Features.Transfers.GetAll;

public static class GetAllTransfersEndpoint
{
    public static void MapGetAllTransfers(this IEndpointRouteBuilder app) =>
        app.MapGet("/", HandleAsync)
            .WithName("Get Transfers")
            .WithSummary("Get all transfers for the current user")
            .WithDescription("Retrieves all transfers across all accounts for the current user")
            .Produces<List<GetTransferResponse>>(StatusCodes.Status200OK);

    public static async Task<IResult> HandleAsync(
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var transfers = await dbContext.Transfers
            .Where(t => t.FromAccount.UserId == currentUser.UserId)
            .OrderByDescending(t => t.TransferDate)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Select(t => new GetTransferResponse(
                t.Id,
                t.FromAccountId,
                t.FromAccount.Name,
                t.ToAccountId,
                t.ToAccount.Name,
                t.FromAmount,
                t.ToAmount,
                t.Note,
                t.TransferDate,
                t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Results.Ok(transfers);
    }
}
