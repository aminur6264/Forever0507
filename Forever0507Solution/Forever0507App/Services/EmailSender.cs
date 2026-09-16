using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Forever0507App.Services;

/// <summary>SMTP settings bound from the "Email" section. Server empty → email sending is off.</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public string Server { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public string FromName { get; set; } = "";
    public bool EnableSsl { get; set; } = true;

    /// <summary>False → the app runs without email; credential sends are skipped with a flash note.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Server) && !string.IsNullOrWhiteSpace(FromAddress);
}

/// <summary>The app's only outbound-email path — login credentials mailed after approval. Failures are logged, never thrown.</summary>
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
            using var client = new SmtpClient(o.Server, o.Port)
            {
                EnableSsl = o.EnableSsl,
            };
            if (!string.IsNullOrWhiteSpace(o.Username))
                client.Credentials = new NetworkCredential(o.Username, o.Password);

            var from = string.IsNullOrWhiteSpace(o.FromName)
                ? new MailAddress(o.FromAddress)
                : new MailAddress(o.FromAddress, o.FromName, System.Text.Encoding.UTF8);
            using var message = new MailMessage(from, new MailAddress(to))
            {
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8,
            };

            // SendMailAsync honours no timeout of its own — cap the whole attempt so an
            // unreachable SMTP server cannot stall the approval request indefinitely.
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await client.SendMailAsync(message, cts.Token);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email to {To} ({Subject}) failed", to, subject);
            return false;
        }
    }
}
