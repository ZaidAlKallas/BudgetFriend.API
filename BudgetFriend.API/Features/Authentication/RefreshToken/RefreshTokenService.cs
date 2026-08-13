using System.Security.Cryptography;
using System.Text;

namespace BudgetFriend.API.Features.Authentication.RefreshToken;

internal sealed class RefreshTokenService(AppDbContext dbContext) : IRefreshTokenService
{
    private static readonly TimeSpan _expiration = TimeSpan.FromDays(30);

    public async Task<(string Token, DateTime ExpiresAtUtc)> GenerateAsync(User user, string jwtId, CancellationToken cancellationToken)
    {
        var rawToken = GenerateTokenValue();
        var tokenHash = HashToken(rawToken);
        var expiresAtUtc = DateTime.UtcNow.Add(_expiration);

        var refreshToken = new Database.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            JwtId = jwtId,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = expiresAtUtc
        };

        dbContext.Set<Database.Entities.RefreshToken>().Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (rawToken, expiresAtUtc);
    }

    public async Task<User?> ConsumeAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(refreshToken);

        var entity = await dbContext.Set<Database.Entities.RefreshToken>()
            .AsTracking()
            .Include(x => x.User)
            .Where(x => x.TokenHash == tokenHash)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null || entity.IsUsed || entity.IsRevoked || entity.ExpiresAtUtc < DateTime.UtcNow)
        {
            return null;
        }

        entity.IsUsed = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return entity.User;
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        await dbContext.Set<Database.Entities.RefreshToken>()
            .Where(x => x.UserId == userId && !x.IsRevoked)
            .ExecuteUpdateAsync(
                s => s.SetProperty(x => x.IsRevoked, true),
                cancellationToken);
    }

    private static string GenerateTokenValue()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
