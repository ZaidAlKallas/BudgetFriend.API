using BudgetFriend.API.Database;
using BudgetFriend.API.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;

public static class TestDb
{
    public static async Task<User?> FindUserAsync(BudgetFriendApiFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.SingleOrDefaultAsync(u => u.NormalizedEmail == email.ToUpperInvariant());
    }

    public static async Task ExpireEmailVerificationAsync(BudgetFriendApiFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.EmailVerificationExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));
    }

    public static async Task ExpirePasswordResetAsync(BudgetFriendApiFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.PasswordResetExpiresAtUtc, DateTime.UtcNow.AddMinutes(-1)));
    }
}