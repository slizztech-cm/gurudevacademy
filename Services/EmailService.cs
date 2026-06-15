using System.Net;
using System.Net.Mail;

namespace GurudevDefenceAcademy.Services;

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlBody);
    Task SendTemplateAsync(string toEmail, string subject, string templateName,
                           Dictionary<string, string> placeholders);
}

public class EmailService(IConfiguration config, IWebHostEnvironment env,
                          ILogger<EmailService> logger) : IEmailService
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var s = config.GetSection("Smtp");
        var host = s["Host"];
        var port = int.TryParse(s["Port"], out var p) ? p : 587;
        var user = s["User"];
        var pass = s["Password"];
        var from = s["From"] ?? user;
        var fromName = s["FromName"] ?? "Gurudev Defence Academy";

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user))
        {
            // Not configured yet — log so local dev still works without SMTP.
            logger.LogWarning("SMTP not configured. Would send to {To}: {Subject}", toEmail, subject);
            return;
        }

        using var msg = new MailMessage
        {
            From = new MailAddress(from!, fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        msg.To.Add(toEmail);

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true
        };
        await client.SendMailAsync(msg);
    }

    public async Task SendTemplateAsync(string toEmail, string subject, string templateName,
                                        Dictionary<string, string> placeholders)
    {
        var path = Path.Combine(env.ContentRootPath, "EmailTemplates", templateName);
        var html = File.Exists(path)
            ? await File.ReadAllTextAsync(path)
            : $"<p>{subject}</p>";

        foreach (var (k, v) in placeholders)
            html = html.Replace("{{" + k + "}}", v);

        await SendAsync(toEmail, subject, html);
    }
}
