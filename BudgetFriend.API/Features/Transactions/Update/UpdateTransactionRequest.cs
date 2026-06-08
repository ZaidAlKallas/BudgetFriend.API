namespace BudgetFriend.API.Features.Transactions.Update;

public sealed record UpdateTransactionRequest(
    decimal Amount,
    string? Note,
    DateTime TransactionDate);
