namespace BudgetFriend.API.Features.Transfers.Create;

public sealed record CreateTransferRequest(
    Guid FromAccountId,
    Guid ToAccountId,
    decimal FromAmount,
    decimal ToAmount,
    string? Note,
    DateTime? TransferDate);
