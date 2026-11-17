using HR.LeaveManagement.Application.Contracts.Email;
using HR.LeaveManagement.Application.Contracts.Logging;
using HR.LeaveManagement.Application.Models.Email;
using HR.LeaveManagement.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using SendGrid;
using HR.LeaveManagement.Infrastructure.Options;
using HR.LeaveManagement.Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HR.LeaveManagement.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailSettings>().Bind(configuration.GetSection("EmailSettings"))
            .Validate(s => !s.Enabled || IsProvider(s, "Console") || IsProvider(s, "Smtp") || IsProvider(s, "SendGrid"),
                "EmailSettings:Provider must be Console, Smtp or SendGrid.")
            .Validate(s => !s.Enabled || MailAddress.TryCreate(s.FromAddress, out _),
                "EmailSettings:FromAddress must be a valid email address when email is enabled.")
            .Validate(s => !s.Enabled || !IsProvider(s, "Smtp") ||
                (!string.IsNullOrWhiteSpace(s.Host) && s.Port is > 0 and <= 65535),
                "SMTP host and a port between 1 and 65535 are required for the Smtp provider.")
            .Validate(s => !s.Enabled || !IsProvider(s, "SendGrid") || !string.IsNullOrWhiteSpace(s.SendGridApiKey),
                "EmailSettings:SendGridApiKey is required for the SendGrid provider.")
            .ValidateOnStart();

        services.AddHttpClient("SendGrid", client => client.Timeout = TimeSpan.FromSeconds(15));
        services.AddTransient<ISendGridClient>(provider => new SendGridClient(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("SendGrid"),
            provider.GetRequiredService<IOptions<EmailSettings>>().Value.SendGridApiKey));

        services.AddTransient<IEmailSender>(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<EmailSettings>>().Value;
            if (!settings.Enabled)
                return ActivatorUtilities.CreateInstance<DisabledEmailSender>(provider);
            if (IsProvider(settings, "Console"))
                return ActivatorUtilities.CreateInstance<ConsoleEmailSender>(provider);
            if (IsProvider(settings, "SendGrid"))
                return ActivatorUtilities.CreateInstance<SendGridEmailSender>(provider);
            return ActivatorUtilities.CreateInstance<SmtpEmailSender>(provider);
        });
        services.AddTransient(typeof(IAppLogger<>), typeof(AppLogger<>));

        services.AddOptions<StatsWorkerOptions>()
            .Bind(configuration.GetSection(StatsWorkerOptions.SectionName))
            .Validate(settings => settings.Interval >= TimeSpan.FromMilliseconds(1) &&
                settings.Interval <= TimeSpan.FromDays(49),
                "StatsWorker:Interval must be between 1 millisecond and 49 days (format hh:mm:ss).")
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddHostedService<ApplicationStatsWorker>();
        return services;
    }

    private static bool IsProvider(EmailSettings settings, string name) =>
        string.Equals(settings.Provider, name, StringComparison.OrdinalIgnoreCase);
}
