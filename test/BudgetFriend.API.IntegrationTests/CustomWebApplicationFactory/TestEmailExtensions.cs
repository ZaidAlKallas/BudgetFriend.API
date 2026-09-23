using System.Text.RegularExpressions;

namespace BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;

public static class TestEmailExtensions
{
    public static string LatestToken(this TestEmailSender sender, string to, string path)
    {
        var token = sender.SentEmails
            .Where(m => m.To.Equals(to, StringComparison.OrdinalIgnoreCase) && m.HtmlBody.Contains(path))
            .Select(m => ExtractToken(m.HtmlBody, path))
            .LastOrDefault();

        return token ?? throw new InvalidOperationException($"No token found in email sent to {to}.");
    }

    public static string LatestCode(this TestEmailSender sender, string to)
    {
        var code = sender.SentEmails
            .Where(m => m.To.Equals(to, StringComparison.OrdinalIgnoreCase))
            .Select(m => ExtractCode(m.HtmlBody))
            .LastOrDefault();

        return code ?? throw new InvalidOperationException($"No code found in email sent to {to}.");
    }

    public static string ExtractToken(string htmlBody, string path)
    {
        var match = Regex.Match(htmlBody, $@"href=""[^""]*{path}\?token=([^&""]+)""");
        return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : string.Empty;
    }

    public static string ExtractCode(string htmlBody)
    {
        var match = Regex.Match(htmlBody, @"<strong>([0-9]{6})</strong>");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}