namespace BudgetFriend.API.Features.Transactions.GetAll;

public sealed record GetTransactionResponse(
    Guid Id,
    Guid AccountId,
    string AccountName,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    string? Note,
    DateTime TransactionDate,
    DateTime CreatedAtUtc);
