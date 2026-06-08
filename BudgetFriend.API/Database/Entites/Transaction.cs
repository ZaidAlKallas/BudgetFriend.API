namespace BudgetFriend.API.Database.Entites;

public sealed class Transaction
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public Guid CategoryId { get; set; }

    public decimal Amount { get; set; }

    public string? Note { get; set; }

    public DateTime TransactionDate { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Account Account { get; set; } = null!;

    public Category Category { get; set; } = null!;
}
