using BudgetFriend.API.Features.Transactions;

namespace BudgetFriend.API.Features.Accounts.GetTransactions;

public sealed record GetAccountTransactionsResponse(
    Guid Id,
    string Name,
    decimal InitialBalance,
    decimal CurrentBalance,
    decimal FilteredBalance,
    Currency Currency,
    PaginatedResult<GetTransactionResponse> Transactions);
