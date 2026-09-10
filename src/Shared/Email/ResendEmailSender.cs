using Microsoft.Extensions.Options;
using Resend;

namespace BudgetFriend.API.Shared.Email;

public class ResendEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly IResend _resend = ResendClient.Create(options.Value.Authorization);
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.Authorization))
        {
            throw new InvalidOperationException("Email:Authorization is not configured.");
        }

        var message = new EmailMessage
        {
            From = new EmailAddress()
            {
                DisplayName = settings.FromName,
                Email = settings.FromAddress
            },
            Subject = subject,
            HtmlBody = htmlBody
        };
        message.To.Add(to);

        await _resend.EmailSendAsync(message, cancellationToken);
    }
}
