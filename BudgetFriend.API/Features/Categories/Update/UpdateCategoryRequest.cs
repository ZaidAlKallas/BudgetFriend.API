using BudgetFriend.API.Database.Enums;

namespace BudgetFriend.API.Features.Categories.Update;

public sealed record UpdateCategoryRequest(string Name, TransactionType TransactionType);
