namespace BudgetFriend.API.Features.Categories;

public sealed record GetCategoryResponse(Guid Id, string Name, TransactionType TransactionType);
