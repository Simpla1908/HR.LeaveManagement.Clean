using HR.LeaveManagement.Application.Contracts.Logging;
using Microsoft.Extensions.Logging;

namespace HR.LeaveManagement.Infrastructure.Services;

public sealed class AppLogger<T>(ILogger<T> logger) : IAppLogger<T>
{
    public void LogInformation(string message, params object[] args) => logger.LogInformation(message, args);
    public void LogWarning(string message, params object[] args) => logger.LogWarning(message, args);
}
