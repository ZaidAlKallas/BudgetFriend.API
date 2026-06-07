namespace BudgetFriend.API.Features.Accounts.Update;

public sealed record UpdateAccountResponse(
    Guid Id,
    string Name,
    decimal InitialBalance);
