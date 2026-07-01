using BudgetFriend.API.Database.Enums;

namespace BudgetFriend.API.Features.Accounts.Create;

public sealed record CreateAccountRequest(string Name, decimal InitialBalance, Currency Currency);
