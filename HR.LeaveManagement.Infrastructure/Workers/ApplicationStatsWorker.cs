using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HR.LeaveManagement.Infrastructure.Workers;

public sealed class ApplicationStatsWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<StatsWorkerOptions> options,
    TimeProvider timeProvider,
    ILogger<ApplicationStatsWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Le worker de statistiques est désactivé.");
            return;
        }

        logger.LogInformation("Worker de statistiques démarré. Premier relevé dans {Interval}, puis à chaque intervalle.", settings.Interval);
        using var timer = new PeriodicTimer(settings.Interval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    // Le worker est singleton ; son DbContext doit appartenir à un nouveau scope.
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var repository = scope.ServiceProvider.GetRequiredService<IApplicationStatsRepository>();
                    var stat = await repository.CaptureAsync(stoppingToken);

                    // ILogger est relié à Serilog : console et fichier, après validation SQL.
                    logger.LogInformation(
                        "Statistiques enregistrées (Id={StatId}, UTC={CapturedAtUtc}) : LeaveAllocation={LeaveAllocationCount}, LeaveRequest={LeaveRequestCount}, LeaveType={LeaveTypeCount}. " +
                        "En attente={PendingLeaveRequestCount} ({PendingLeaveRequestPercentage:0.00} %, durée moyenne={PendingLeaveRequestAverageDays:0.00} jours), " +
                        "Approuvées={ApprovedLeaveRequestCount} ({ApprovedLeaveRequestPercentage:0.00} %, durée moyenne={ApprovedLeaveRequestAverageDays:0.00} jours), " +
                        "Annulées={CancelledLeaveRequestCount} ({CancelledLeaveRequestPercentage:0.00} %, durée moyenne={CancelledLeaveRequestAverageDays:0.00} jours).",
                        stat.Id, stat.CapturedAtUtc, stat.LeaveAllocationCount, stat.LeaveRequestCount, stat.LeaveTypeCount,
                        stat.PendingLeaveRequestCount, stat.PendingLeaveRequestPercentage, stat.PendingLeaveRequestAverageDays,
                        stat.ApprovedLeaveRequestCount, stat.ApprovedLeaveRequestPercentage, stat.ApprovedLeaveRequestAverageDays,
                        stat.CancelledLeaveRequestCount, stat.CancelledLeaveRequestPercentage, stat.CancelledLeaveRequestAverageDays);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Échec du relevé de statistiques. Nouvelle tentative au prochain intervalle.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Arrêt normal de l'API : interrompre l'attente sans journaliser d'erreur.
        }
    }
}
