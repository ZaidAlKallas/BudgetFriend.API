namespace BudgetFriend.API.Features.Authentication.PasswordReset;

internal static class PasswordResetDefaults
{
    public const int CodeLength = 6;

    public const int MaxAttempts = 5;

    public static readonly TimeSpan Expiry = TimeSpan.FromMinutes(15);
}