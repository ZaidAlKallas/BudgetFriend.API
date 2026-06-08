namespace BudgetFriend.API.Database.Entites;

public sealed class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }

    public string? PasswordHash { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Account> Accounts { get; set; } = [];

    public ICollection<Category> Categories { get; set; } = [];
}
