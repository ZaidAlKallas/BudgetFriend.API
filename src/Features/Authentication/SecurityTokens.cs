using System.Security.Cryptography;
using System.Text;

namespace BudgetFriend.API.Features.Authentication;

internal static class SecurityTokens
{
    public static string GenerateNumericCode(int length = 6)
    {
        var maxExclusive = (int)Math.Pow(10, length);
        return RandomNumberGenerator.GetInt32(0, maxExclusive).ToString("D" + length);
    }

    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}