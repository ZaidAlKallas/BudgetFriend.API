using System.Text.RegularExpressions;

namespace BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;

public static class TestEmailExtensions
{
    public static string LatestCode(this TestEmailSender sender, string to, string subject)
    {
        var code = sender.SentEmails
            .Where(m => m.To.Equals(to, StringComparison.OrdinalIgnoreCase) && m.Subject.Equals(subject, StringComparison.OrdinalIgnoreCase))
            .Select(m => ExtractCode(m.HtmlBody))
            .LastOrDefault();

        return code ?? throw new InvalidOperationException($"No code found in email sent to {to} with subject {subject}.");
    }

    public static string ExtractCode(string htmlBody)
    {
        var match = Regex.Match(htmlBody, @"<strong>([0-9]{6})</strong>");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}