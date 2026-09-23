namespace BudgetFriend.API.Features.Authentication.EmailVerification;

internal static class EmailVerificationDefaults
{
    public const int CodeLength = 6;

    public const int MaxAttempts = 5;

    public static readonly TimeSpan Expiry = TimeSpan.FromMinutes(15);
}