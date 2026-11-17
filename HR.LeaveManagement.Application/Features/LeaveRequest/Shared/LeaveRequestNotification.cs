using HR.LeaveManagement.Application.Contracts.Email;
using HR.LeaveManagement.Application.Contracts.Identity;
using HR.LeaveManagement.Application.Contracts.Logging;
using HR.LeaveManagement.Application.Models.Email;

namespace HR.LeaveManagement.Application.Features.LeaveRequest.Shared;

public sealed class LeaveRequestNotification(
    IUserService user,
    IEmailSender emailSender,
    IAppLogger<LeaveRequestNotification> logger)
{
    // À appeler après la transaction : une panne email ne doit pas annuler l'opération.
    public async Task SendAsync(string employeeId, string subject)
    {
        try
        {
            var employee = await user.GetEmployee(employeeId);
            if (string.IsNullOrWhiteSpace(employee?.Email))
            {
                logger.LogWarning("Leave notification skipped: employee {EmployeeId} has no email address.", employeeId);
                return;
            }

            var email = new EmailMessage
            {
                To = employee.Email,
                Subject = subject,
                Body = "Your leave request has been updated. Sign in to view its details."
            };

            var sent = await emailSender.SendEmail(email);
            if (!sent)
            {
                logger.LogWarning("Leave notification was not sent for employee {EmployeeId}.", employeeId);
            }
            else
            {
                logger.LogInformation("Leave notification processed for employee {EmployeeId}.", employeeId);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Leave notification failed for employee {EmployeeId}; the business transaction remains committed.", employeeId);
        }
    }
}
