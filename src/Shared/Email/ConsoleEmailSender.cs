using Microsoft.Extensions.Options;

namespace BudgetFriend.API.Shared.Email;

public sealed class ConsoleEmailSender(IOptions<EmailOptions> options, ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var from = string.IsNullOrWhiteSpace(options.Value.FromAddress)
            ? "no-reply@budgetfriend.local"
            : options.Value.FromAddress;

        logger.LogInformation(
            "Email sent to {To} from {From} with subject {Subject} (ConsoleEmailSender): {Body}",
            to,
            from,
            subject,
            htmlBody);

        return Task.CompletedTask;
    }
}