using BudgetFriend.API.Shared.Email;
using Microsoft.Extensions.Options;

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

    public static string BuildPasswordResetMessage(IOptions<EmailOptions> options, string token)
    {
        var link = BuildLink(options, "/reset-password", token);

        return "<p>We received a request to reset your password.</p>" +
               "<p>Click the link below to choose a new password:</p>" +
               $"<p><a href=\"{link}\">Reset Password</a></p>" +
               "<p>If you did not request this, you can ignore this email.</p>";
    }

    private static string BuildLink(IOptions<EmailOptions> options, string path, string token)
    {
        var deepLinkBaseUrl = options.Value.DeepLinkBaseUrl?.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(deepLinkBaseUrl))
        {
            return $"{deepLinkBaseUrl}{path}?token={Uri.EscapeDataString(token)}";
        }

        var baseUrl = options.Value.BaseUrl.TrimEnd('/');
        return $"{baseUrl}{path}?token={Uri.EscapeDataString(token)}";
    }
}