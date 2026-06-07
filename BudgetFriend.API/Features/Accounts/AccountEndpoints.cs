using BudgetFriend.API.Features.Accounts.Create;
using BudgetFriend.API.Features.Accounts.Delete;
using BudgetFriend.API.Features.Accounts.GetAll;
using BudgetFriend.API.Features.Accounts.GetById;
using BudgetFriend.API.Features.Accounts.Update;

namespace BudgetFriend.API.Features.Accounts;

public static class AccountEndpoints {
    public static WebApplication MapAccountEndpoints(this WebApplication app) {
        var group = app.MapGroup("/api/accounts")
            .RequireAuthorization()
            .WithTags("Accounts")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapCreateAccount();
        group.MapGetAllAccounts();
        group.MapGetAccountById();
        group.MapUpdateAccount();
        group.MapDeleteAccount();

        return app;
    }
}
