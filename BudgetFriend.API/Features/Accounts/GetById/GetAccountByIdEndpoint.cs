using BudgetFriend.API.Database;
using BudgetFriend.API.Features.Authentication;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Accounts.GetById;

public static class GetAccountByIdEndpoint
{
    public static void MapGetAccountById(this IEndpointRouteBuilder app) =>
        app.MapGet("/{accountId}", HandleAsync)
            .WithName("Get Account by Id")
            .WithSummary("Get an account by its ID")
            .WithDescription("Retrieves an account associated with the specified ID")
            .Produces<GetAccountResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

    public static async Task<IResult> HandleAsync(
        Guid accountId,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var account = await dbContext.Accounts
            .Where(a => a.UserId == currentUser.UserId && a.Id == accountId)
            .Select(a => new GetAccountResponse(
                a.Id,
                a.Name,
                a.InitialBalance))
            .FirstOrDefaultAsync(cancellationToken);
        return account is not null ? Results.Ok(account) : Results.NotFound();
    }
}
