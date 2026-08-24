namespace BudgetFriend.API.Features.Transfers;

public sealed record GetTransferResponse(
    Guid Id,
    Guid FromAccountId,
    string FromAccountName,
    Guid ToAccountId,
    string ToAccountName,
    decimal FromAmount,
    decimal ToAmount,
    string? Note,
    DateTime TransferDate,
    DateTime CreatedAtUtc);
