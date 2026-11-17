using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Domain;
using HR.LeaveManagement.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;

namespace HR.LeaveManagement.Persistence.Repositories;

public sealed class ApplicationStatsRepository(HrDatabaseContext context, IUnitOfWork unitOfWork)
    : IApplicationStatsRepository
{
    public Task<ApplicationStat> CaptureAsync(CancellationToken cancellationToken = default) =>
        // La transaction sérialisable rend les trois compteurs cohérents entre eux.
        unitOfWork.ExecuteAsync(async () =>
        {
            var capturedAtUtc = DateTime.UtcNow;
            // L'annulation prime sur l'ancienne décision : les catégories sont disjointes.
            // SQL calcule les agrégats sans charger toutes les demandes en mémoire.
            var requestsByStatus = await context.LeaveRequests
                .GroupBy(request => request.Cancelled ? "Cancelled"
                    : request.Approved == true ? "Approved"
                    : request.Approved == null ? "Pending" : "Rejected")
                .Select(group => new
                {
                    Status = group.Key,
                    Count = group.Count(),
                    AverageDays = group.Average(request =>
                        (decimal)(EF.Functions.DateDiffDay(request.StartDate, request.EndDate) + 1))
                })
                .ToListAsync(cancellationToken);

            var pending = requestsByStatus.SingleOrDefault(group => group.Status == "Pending");
            var approved = requestsByStatus.SingleOrDefault(group => group.Status == "Approved");
            var cancelled = requestsByStatus.SingleOrDefault(group => group.Status == "Cancelled");
            var total = requestsByStatus.Sum(group => group.Count);
            var stat = new ApplicationStat
            {
                CapturedAtUtc = capturedAtUtc,
                LeaveTypeCount = await context.LeaveTypes.CountAsync(cancellationToken),
                LeaveAllocationCount = await context.LeaveAllocations.CountAsync(cancellationToken),
                LeaveRequestCount = total,
                PendingLeaveRequestCount = pending?.Count ?? 0,
                ApprovedLeaveRequestCount = approved?.Count ?? 0,
                CancelledLeaveRequestCount = cancelled?.Count ?? 0,
                PendingLeaveRequestAverageDays = RoundDays(pending?.AverageDays),
                ApprovedLeaveRequestAverageDays = RoundDays(approved?.AverageDays),
                CancelledLeaveRequestAverageDays = RoundDays(cancelled?.AverageDays),
                PendingLeaveRequestPercentage = Percentage(pending?.Count ?? 0, total),
                ApprovedLeaveRequestPercentage = Percentage(approved?.Count ?? 0, total),
                CancelledLeaveRequestPercentage = Percentage(cancelled?.Count ?? 0, total)
            };

            context.Stats.Add(stat);
            await context.SaveChangesAsync(cancellationToken);
            return stat;
        }, cancellationToken);

    private static decimal? RoundDays(decimal? days) =>
        days.HasValue ? Math.Round(days.Value, 2, MidpointRounding.AwayFromZero) : null;

    private static decimal Percentage(int count, int total) =>
        total == 0 ? 0m : Math.Round(count * 100m / total, 2, MidpointRounding.AwayFromZero);
}
