using HR.LeaveManagement.Application.Contracts.Email;
using HR.LeaveManagement.Application.Models.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace HR.LeaveManagement.Infrastructure.Services;

public sealed class SendGridEmailSender(
    ISendGridClient client,
    IOptions<EmailSettings> options,
    ILogger<SendGridEmailSender> logger) : IEmailSender
{
    public async Task<bool> SendEmail(EmailMessage email)
    {
        var settings = options.Value;
        var message = MailHelper.CreateSingleEmail(
            new EmailAddress(settings.FromAddress, settings.FromName),
            new EmailAddress(email.To), email.Subject, email.Body, htmlContent: null);

        var response = await client.SendEmailAsync(message);
        // Le corps de la réponse peut contenir des données du message : ne pas le journaliser.
        using (response.Body)
        {
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("SendGrid rejected the email with HTTP status {StatusCode}.", (int)response.StatusCode);
                return false;
            }
        }

        logger.LogInformation("SendGrid accepted the email for delivery.");
        return true;
    }
}
