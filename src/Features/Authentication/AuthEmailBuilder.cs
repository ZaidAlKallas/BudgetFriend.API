using BudgetFriend.API.Shared.Email;
using Microsoft.Extensions.Options;

namespace BudgetFriend.API.Features.Authentication;

internal static class AuthEmailBuilder
{
    public static string BuildVerificationMessage(IOptions<EmailOptions> options, string token)
    {
        var link = BuildLink(options, "/verify-email", token);

        return $"<p>Welcome to BudgetFriend!</p>" +
               $"<p>Please verify your email address by clicking the link below:</p>" +
               $"<p><a href=\"{link}\">Verify Email</a></p>" +
               "<p>If you did not create an account, you can ignore this email.</p>";
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
        var baseUrl = options.Value.BaseUrl.TrimEnd('/');
        return $"{baseUrl}{path}?token={Uri.EscapeDataString(token)}";
    }
}