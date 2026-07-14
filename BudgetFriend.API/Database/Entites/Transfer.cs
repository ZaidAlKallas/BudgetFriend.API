namespace BudgetFriend.API.Database.Entites;

public sealed class Transfer
{
    public Guid Id { get; set; }

    public Guid FromAccountId { get; set; }

    public Guid ToAccountId { get; set; }

    public Guid OutgoingTransactionId { get; set; }

    public Guid IncomingTransactionId { get; set; }

    public decimal FromAmount { get; set; }

    public decimal ToAmount { get; set; }

    public DateTime TransferDate { get; set; } = DateTime.UtcNow;

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Account FromAccount { get; set; } = null!;

    public Account ToAccount { get; set; } = null!;

    public Transaction OutgoingTransaction { get; set; } = null!;

    public Transaction IncomingTransaction { get; set; } = null!;
}
