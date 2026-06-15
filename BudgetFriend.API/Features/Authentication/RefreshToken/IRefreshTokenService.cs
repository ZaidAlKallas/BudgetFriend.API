using BudgetFriend.API.Database.Entites;

namespace BudgetFriend.API.Features.Authentication.RefreshToken;

public interface IRefreshTokenService
{
    Task<(string Token, DateTime ExpiresAtUtc)> GenerateAsync(User user, string jwtId, CancellationToken cancellationToken);
    Task<User?> ConsumeAsync(string refreshToken, CancellationToken cancellationToken);
    Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken);
}
