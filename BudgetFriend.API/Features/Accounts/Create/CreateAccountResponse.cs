using BudgetFriend.API.Database.Enums;

namespace BudgetFriend.API.Features.Accounts.Create;

public sealed record CreateAccountResponse(
    Guid Id,
    string Name,
    decimal InitialBalance,
    Currency Currency);
