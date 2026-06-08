using BudgetFriend.API.Database.Enums;

namespace BudgetFriend.API.Features.Categories.Create;

public sealed record CreateCategoryResponse(Guid Id, string Name, TransactionType TransactionType);
