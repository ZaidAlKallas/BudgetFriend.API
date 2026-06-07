namespace BudgetFriend.API.Features.Accounts;

public sealed record GetAccountResponse(
    Guid Id,
    string Name,
    decimal InitialBalance);


