namespace BudgetFriend.API.Features.Transactions;

public sealed record GetTransactionResponse(
    Guid Id,
    Guid AccountId,
    string AccountName,
    Guid? CategoryId,
    string? CategoryName,
    TransactionType TransactionType,
    Currency Currency,
    decimal Amount,
    string? Note,
    DateTime TransactionDate,
    DateTime CreatedAtUtc);
