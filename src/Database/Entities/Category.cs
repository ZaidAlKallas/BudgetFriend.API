namespace BudgetFriend.API.Database.Entities;

public sealed class Category
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public TransactionType TransactionType { get; set; }

    public User User { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = [];
}
