namespace BudgetFriend.API.Features.Accounts.GetAll;

public static class GetAllAccountsEndpoint
{
    public static void MapGetAllAccounts(this IEndpointRouteBuilder app) =>
        app.MapGet("/", HandleAsync)
            .WithName("Get Accounts")
            .WithSummary("Get all accounts for the current user")
            .WithDescription("Retrieves a list of all accounts associated with the current user")
            .Produces<List<GetAccountResponse>>(StatusCodes.Status200OK);

    public static async Task<IResult> HandleAsync(
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var accounts = await dbContext.Accounts
            .Where(a => a.UserId == currentUser.UserId)
            .OrderBy(a => a.Name)
            .Select(a => new GetAccountResponse(
                a.Id,
                a.Name,
                a.InitialBalance,
                a.Transactions.Sum(t =>
                    t.TransactionType == TransactionType.Income ||
                    t.TransactionType == TransactionType.TransferIn
                        ? t.Amount
                        : -t.Amount) + a.InitialBalance,
                a.Currency))
            .ToListAsync(cancellationToken);
        return Results.Ok(accounts);
    }
}

