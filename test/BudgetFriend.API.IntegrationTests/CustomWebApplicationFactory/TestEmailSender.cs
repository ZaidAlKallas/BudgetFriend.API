using BudgetFriend.API.Shared.Email;

namespace BudgetFriend.API.IntegrationTests.CustomWebApplicationFactory;

public sealed class TestEmailSender : IEmailSender
{
    public List<EmailMessage> SentEmails { get; } = [];

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        SentEmails.Add(new EmailMessage(to, subject, htmlBody));
        return Task.CompletedTask;
    }
}

public sealed record EmailMessage(string To, string Subject, string HtmlBody);