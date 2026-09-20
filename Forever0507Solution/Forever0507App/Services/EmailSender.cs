using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Forever0507App.Services;

/// <summary>SMTP settings bound from the "Email" section. Server empty → email sending is off.</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public string Server { get; set; } = "";
    public int Port { get; set; } = 465;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "";
    public bool EnableSsl { get; set; } = true;

    /// <summary>False → the app runs without email; credential sends are skipped with a flash note.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Server) && !string.IsNullOrWhiteSpace(FromAddress);
}

/// <summary>The app's only outbound-email path — acknowledgment and credential mails. Failures are logged, never thrown.
/// MailKit instead of System.Net.Mail.SmtpClient because typical mail-host SMTP (465) is implicit TLS,
/// which SmtpClient cannot do (its EnableSsl is STARTTLS-only, i.e. port 587).</summary>
public class EmailSender(IOptions<EmailOptions> options, ILogger<EmailSender> logger)
{
    public bool IsConfigured => options.Value.IsConfigured;

    /// <summary>True when the message went out; false on any failure or when disabled/unconfigured.</summary>
    public async Task<bool> SendAsync(string to, string subject, string htmlBody)
    {
        var o = options.Value;
        if (!o.IsConfigured || string.IsNullOrWhiteSpace(to)) return false;

        try
        {
            // 465 expects TLS from the first byte; other ports negotiate STARTTLS when offered.
            var secure = !o.EnableSsl
                ? SecureSocketOptions.None
                : o.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;

            var from = new MailboxAddress(string.IsNullOrWhiteSpace(o.FromName) ? null : o.FromName, o.FromAddress);
            var message = new MimeMessage();
            message.From.Add(from);
            message.To.Add(new MailboxAddress(null, to.Trim()));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(o.Server, o.Port, secure);
            if (!string.IsNullOrWhiteSpace(o.Username))
                await client.AuthenticateAsync(new NetworkCredential(o.Username, o.Password));

            // SendAsync honours no timeout of its own — cap the whole attempt so an
            // unreachable SMTP server cannot stall the registration/approval request indefinitely.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await client.SendAsync(message, cts.Token);
            await client.DisconnectAsync(true, cts.Token);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email to {To} ({Subject}) failed", to, subject);
            return false;
        }
    }
}
