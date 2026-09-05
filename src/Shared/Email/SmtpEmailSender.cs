using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace BudgetFriend.API.Shared.Email;

public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            throw new InvalidOperationException("Email:SmtpHost is not configured.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = settings.SmtpEnableSsl,
            Credentials = string.IsNullOrWhiteSpace(settings.SmtpUsername)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword)
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
