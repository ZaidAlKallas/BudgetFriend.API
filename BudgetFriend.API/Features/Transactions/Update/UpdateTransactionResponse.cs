namespace BudgetFriend.API.Features.Transactions.Update;

public sealed record UpdateTransactionResponse(
    Guid Id,
    Guid AccountId,
    Guid CategoryId,
    decimal Amount,
    string? Note,
    DateTime TransactionDate,
    DateTime CreatedAtUtc);
