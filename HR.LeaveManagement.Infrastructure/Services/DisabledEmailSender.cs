using HR.LeaveManagement.Application.Contracts.Email;
using HR.LeaveManagement.Application.Models.Email;
using Microsoft.Extensions.Logging;

namespace HR.LeaveManagement.Infrastructure.Services;

public sealed class DisabledEmailSender(ILogger<DisabledEmailSender> logger) : IEmailSender
{
    public Task<bool> SendEmail(EmailMessage email)
    {
        logger.LogDebug("Email delivery is disabled by configuration.");
        return Task.FromResult(false);
    }
}
