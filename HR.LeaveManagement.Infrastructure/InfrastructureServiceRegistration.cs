using HR.LeaveManagement.Application.Contracts.Email;
using HR.LeaveManagement.Application.Contracts.Logging;
using HR.LeaveManagement.Application.Models.Email;
using HR.LeaveManagement.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HR.LeaveManagement.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailSettings>().Bind(configuration.GetSection("EmailSettings"))
            .Validate(s => !s.Enabled || (!string.IsNullOrWhiteSpace(s.Host) &&
                !string.IsNullOrWhiteSpace(s.FromAddress) && s.Port is > 0 and <= 65535),
                "SMTP host, port and sender are required when email is enabled.")
            .ValidateOnStart();
        services.AddTransient<IEmailSender, SmtpEmailSender>();
        services.AddTransient(typeof(IAppLogger<>), typeof(AppLogger<>));
        return services;
    }
}
