using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using PromptStash.Api.Common.Settings;

namespace PromptStash.Api.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public sealed class SmtpEmailService(IOptions<EmailOptions> options, ILogger<SmtpEmailService> logger)
    : IEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (_options.LogOnly)
        {
            logger.LogInformation(
                "[Email LogOnly] To: {To} | Subject: {Subject}\n{Body}",
                to, subject, htmlBody);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port,
            _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);

        if (!string.IsNullOrWhiteSpace(_options.Username))
            await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
