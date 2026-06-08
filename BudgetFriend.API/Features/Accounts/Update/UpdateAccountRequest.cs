namespace BudgetFriend.API.Features.Accounts.Update;

public sealed record UpdateAccountRequest(
    string Name,
    decimal InitialBalance);
