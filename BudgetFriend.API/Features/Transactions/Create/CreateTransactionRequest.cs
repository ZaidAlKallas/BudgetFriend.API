namespace BudgetFriend.API.Features.Transactions.Create;

public sealed record CreateTransactionRequest(
    Guid AccountId,
    Guid CategoryId,
    decimal Amount,
    string? Note,
    DateTime? TransactionDate);
