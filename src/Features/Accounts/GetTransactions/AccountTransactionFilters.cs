namespace BudgetFriend.API.Features.Accounts.GetTransactions;

public sealed record AccountTransactionFilters(
    int PageNumber = 1,
    int PageSize = 20,
    Guid? CategoryId = null,
    TransactionType? TransactionType = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    string? Search = null) : PaginationRequest(PageNumber, PageSize);
