namespace BudgetFriend.API.Features.Transactions.Create;

public sealed record CreateTransactionResponse(
    Guid Id,
    Guid AccountId,
    Guid CategoryId,
    decimal Amount,
    string? Note,
    DateTime TransactionDate,
    DateTime CreatedAtUtc);
