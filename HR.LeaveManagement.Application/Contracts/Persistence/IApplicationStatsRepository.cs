using HR.LeaveManagement.Domain;

namespace HR.LeaveManagement.Application.Contracts.Persistence;

public interface IApplicationStatsRepository
{
    Task<ApplicationStat> CaptureAsync(CancellationToken cancellationToken = default);
}
