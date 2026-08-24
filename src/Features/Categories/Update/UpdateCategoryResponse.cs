namespace BudgetFriend.API.Features.Categories.Update;

public sealed record UpdateCategoryResponse(Guid Id, string Name, TransactionType TransactionType);
