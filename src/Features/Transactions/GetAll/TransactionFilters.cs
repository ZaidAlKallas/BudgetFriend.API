namespace BudgetFriend.API.Features.Transactions.GetAll;

public sealed record TransactionFilters(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? AccountId = null,
    Guid? CategoryId = null,
    TransactionType? TransactionType = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Search = null) : PaginationRequest(PageNumber, PageSize);
