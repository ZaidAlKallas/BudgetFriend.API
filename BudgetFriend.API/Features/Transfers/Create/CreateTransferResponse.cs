namespace BudgetFriend.API.Features.Transfers.Create;

public sealed record CreateTransferResponse(
    Guid Id,
    Guid FromAccountId,
    Guid ToAccountId,
    decimal FromAmount,
    decimal ToAmount,
    string? Note,
    DateTime TransferDate,
    DateTime CreatedAtUtc);
