namespace BudgetFriend.API.Shared.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; init; } = string.Empty;

    public string? FromName { get; init; }

    public string BaseUrl { get; init; } = string.Empty;

    public string Authorization { get; set; } = string.Empty;

    public bool UseConsoleEmailSender { get; init; } = true;

    public string? SmtpHost { get; init; }

    public int SmtpPort { get; init; } = 587;

    public string? SmtpUsername { get; init; }

    public string? SmtpPassword { get; init; }

    public bool SmtpEnableSsl { get; init; } = true;
}
