namespace BudgetFriend.API.Features.Transfers.GetById;

public static class GetTransferByIdEndpoint
{
    public static void MapGetTransferById(this IEndpointRouteBuilder app) =>
        app.MapGet("/{transferId}", HandleAsync)
            .WithName("Get Transfer by Id")
            .WithSummary("Get a transfer by its ID")
            .WithDescription("Retrieves a transfer associated with the specified ID")
            .Produces<GetTransferResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

    public static async Task<IResult> HandleAsync(
        Guid transferId,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var transfer = await dbContext.Transfers
            .Where(t => t.FromAccount.UserId == currentUser.UserId && t.Id == transferId)
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
            .FirstOrDefaultAsync(cancellationToken);

        return transfer is not null ? Results.Ok(transfer) : Results.NotFound();
    }
}
