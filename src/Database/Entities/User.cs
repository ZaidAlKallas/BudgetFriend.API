namespace BudgetFriend.API.Database.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }

    public DateTime? EmailVerifiedAtUtc { get; set; }

    public string? EmailVerificationTokenHash { get; set; }

    public DateTime? EmailVerificationExpiresAtUtc { get; set; }

    public string? PasswordResetTokenHash { get; set; }

    public DateTime? PasswordResetExpiresAtUtc { get; set; }

    public string? GoogleSubject { get; set; }

    public string? PasswordHash { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Account> Accounts { get; set; } = [];

    public ICollection<Category> Categories { get; set; } = [];

    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
