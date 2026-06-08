using BudgetFriend.API.Database.Enums;

namespace BudgetFriend.API.Features.Categories.Create;

public sealed record CreateCategoryRequest(string Name, TransactionType TransactionType);
