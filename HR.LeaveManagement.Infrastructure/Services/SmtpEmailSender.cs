using System.Net;
using System.Net.Mail;
using HR.LeaveManagement.Application.Contracts.Email;
using HR.LeaveManagement.Application.Models.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HR.LeaveManagement.Infrastructure.Services;

public sealed class SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task<bool> SendEmail(EmailMessage email)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Email delivery is disabled by configuration.");
            return false;
        }
        using var message = new MailMessage
        {
            From = new MailAddress(settings.FromAddress, settings.FromName),
            Subject = email.Subject, Body = email.Body
        };
        message.To.Add(new MailAddress(email.To));
        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            UseDefaultCredentials = false
        };
        if (!string.IsNullOrWhiteSpace(settings.UserName))
            client.Credentials = new NetworkCredential(settings.UserName, settings.Password);
        await client.SendMailAsync(message);
        return true;
    }
}
