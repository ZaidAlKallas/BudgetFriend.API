namespace BudgetFriend.API.Features.Authentication;

internal static class AuthEmailBuilder
{
    public static string BuildVerificationCodeMessage(string code)
    {
        return $"<p>Welcome to BudgetFriend!</p>" +
               $"<p>Your email verification code is:</p>" +
               $"<p><strong>{code}</strong></p>" +
               "<p>This code expires in 15 minutes. If you did not create an account, you can ignore this email.</p>";
    }

    public static string BuildPasswordResetCodeMessage(string code)
    {
        return "<p>We received a request to reset your password.</p>" +
               $"<p>Your password reset code is:</p>" +
               $"<p><strong>{code}</strong></p>" +
               "<p>This code expires in 15 minutes. If you did not request this, you can ignore this email.</p>";
    }
}