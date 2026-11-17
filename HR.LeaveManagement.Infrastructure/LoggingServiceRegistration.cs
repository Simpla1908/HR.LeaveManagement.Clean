using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Json;

namespace HR.LeaveManagement.Infrastructure;

public static class LoggingServiceRegistration
{
    public static IServiceCollection AddInfrastructureLogging(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        return services.AddSerilog((provider, logger) => logger
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(provider)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", environment.ApplicationName)
            .Enrich.WithProperty("Environment", environment.EnvironmentName)
            .WriteTo.File(
                new JsonFormatter(renderMessage: true),
                Path.GetFullPath(configuration["LogFiles:Path"] ?? "Logs/hr-leave-.json", environment.ContentRootPath),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 10_000_000,
                rollOnFileSizeLimit: true,
                shared: true));
    }
}
