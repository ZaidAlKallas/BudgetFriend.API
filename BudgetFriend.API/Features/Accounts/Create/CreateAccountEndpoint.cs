using BudgetFriend.API.Database;
using BudgetFriend.API.Database.Entites;
using BudgetFriend.API.Features.Authentication;
using BudgetFriend.API.Shared.Validation;
using Microsoft.EntityFrameworkCore;

namespace BudgetFriend.API.Features.Accounts.Create;

public static class CreateAccountEndpoint {
    public static void MapCreateAccount(this IEndpointRouteBuilder app) =>
        app.MapPost("/", HandleAsync)
            .WithValidation<CreateAccountRequest>()
            .WithName("Create Account")
            .WithSummary("Create a new account")
            .WithDescription("Creates a new account using name and initial balance")
            .Produces<CreateAccountResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict);

    private static async Task<IResult> HandleAsync(
        CreateAccountRequest request,
        AppDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken cancellationToken) {
        var account = new Account {
            Id = Guid.NewGuid(),
            UserId = currentUser.UserId,
            Name = request.Name.Trim(),
            InitialBalance = request.InitialBalance,
        };

        var exists = await dbContext.Accounts
                .AnyAsync(a => a.Name == request.Name && a.UserId == currentUser.UserId, cancellationToken);

        if (exists)
            return Results.Conflict($"A account with this name already exists.");

        await dbContext.Accounts.AddAsync(account, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/accounts/{account.Id}",
            new CreateAccountResponse(
                account.Id,
                account.Name,
                account.InitialBalance));
    }
}
