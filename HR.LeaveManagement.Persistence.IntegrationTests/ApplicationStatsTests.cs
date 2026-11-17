using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Domain;
using HR.LeaveManagement.Persistence.DatabaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace HR.LeaveManagement.Persistence.IntegrationTests;

public sealed class ApplicationStatsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Captures_persist_current_counts_and_keep_history()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetAsync();
        await fixture.SeedRequestAsync();
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HrDatabaseContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IApplicationStatsRepository>();
        await db.Stats.ExecuteDeleteAsync(cancellationToken);
        var before = DateTime.UtcNow;

        var first = await repository.CaptureAsync(cancellationToken);
        Assert.True(first.Id > 0);
        Assert.Equal(3, first.LeaveAllocationCount);
        Assert.Equal(1, first.LeaveRequestCount);
        Assert.Equal(1, first.LeaveTypeCount);
        Assert.Equal(1, first.PendingLeaveRequestCount);
        Assert.Equal(3m, first.PendingLeaveRequestAverageDays);
        Assert.Equal(100m, first.PendingLeaveRequestPercentage);
        Assert.Equal(0, first.ApprovedLeaveRequestCount);
        Assert.Null(first.ApprovedLeaveRequestAverageDays);
        Assert.Equal(0m, first.ApprovedLeaveRequestPercentage);
        Assert.Equal(0, first.CancelledLeaveRequestCount);
        Assert.Null(first.CancelledLeaveRequestAverageDays);
        Assert.Equal(0m, first.CancelledLeaveRequestPercentage);
        Assert.InRange(first.CapturedAtUtc, before, DateTime.UtcNow);

        var leaveType = new LeaveType { Name = "Study", DefaultDays = 5 };
        db.LeaveTypes.Add(leaveType);
        await db.SaveChangesAsync(cancellationToken);
        db.LeaveAllocations.Add(new LeaveAllocation
        { EmployeeId = fixture.EmployeeId, LeaveTypeId = leaveType.Id, Period = 2030, NumberOfDays = 5 });
        await db.SaveChangesAsync(cancellationToken);

        var second = await repository.CaptureAsync(cancellationToken);
        Assert.Equal(4, second.LeaveAllocationCount);
        Assert.Equal(1, second.LeaveRequestCount);
        Assert.Equal(2, second.LeaveTypeCount);
        Assert.NotEqual(first.Id, second.Id);

        var history = await db.Stats.AsNoTracking().OrderBy(stat => stat.Id).ToListAsync(cancellationToken);
        Assert.Equal(2, history.Count);
        Assert.Equal(3, history[0].LeaveAllocationCount);
        Assert.Equal(4, history[1].LeaveAllocationCount);
        Assert.Equal("stats", db.Model.FindEntityType(typeof(ApplicationStat))!.GetTableName());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task Status_statistics_exclude_cancelled_requests_from_other_statuses_and_include_rejections_in_the_total()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetAsync();
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HrDatabaseContext>();
        await db.Stats.ExecuteDeleteAsync(cancellationToken);
        db.LeaveRequests.AddRange(
            Request(null, false, 1), Request(null, false, 2),
            Request(true, false, 2), Request(true, false, 5),
            Request(true, true, 3), Request(null, true, 4),
            Request(false, false, 30));
        await db.SaveChangesAsync(cancellationToken);

        var stat = await scope.ServiceProvider.GetRequiredService<IApplicationStatsRepository>().CaptureAsync(cancellationToken);
        db.ChangeTracker.Clear();
        var saved = await db.Stats.AsNoTracking().SingleAsync(cancellationToken);

        Assert.Equal(7, stat.LeaveRequestCount);
        Assert.Equal(2, saved.PendingLeaveRequestCount);
        Assert.Equal(2, saved.ApprovedLeaveRequestCount);
        Assert.Equal(2, saved.CancelledLeaveRequestCount);
        Assert.Equal(1.50m, saved.PendingLeaveRequestAverageDays);
        Assert.Equal(3.50m, saved.ApprovedLeaveRequestAverageDays);
        Assert.Equal(3.50m, saved.CancelledLeaveRequestAverageDays);
        Assert.Equal(28.57m, saved.PendingLeaveRequestPercentage);
        Assert.Equal(28.57m, saved.ApprovedLeaveRequestPercentage);
        Assert.Equal(28.57m, saved.CancelledLeaveRequestPercentage);
    }

    [Fact]
    public async Task Average_durations_are_rounded_to_two_decimal_places()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await fixture.ResetAsync();
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HrDatabaseContext>();
        db.LeaveRequests.AddRange(Request(null, false, 1), Request(null, false, 1), Request(null, false, 2));
        await db.SaveChangesAsync(cancellationToken);
        var stat = await scope.ServiceProvider.GetRequiredService<IApplicationStatsRepository>().CaptureAsync(cancellationToken);
        db.ChangeTracker.Clear();
        var saved = await db.Stats.AsNoTracking().SingleAsync(row => row.Id == stat.Id, cancellationToken);
        Assert.Equal(1.33m, saved.PendingLeaveRequestAverageDays);
        Assert.Equal(100m, saved.PendingLeaveRequestPercentage);
    }

    [Fact]
    public async Task Migration_preserves_historical_snapshots_without_inventing_status_measurements()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HrDatabaseContext>();
        // Les changements de schéma de ce test restent limités à la base temporaire du fixture.
        Assert.StartsWith("HRLeaveTests_", db.Database.GetDbConnection().Database);
        await db.Stats.ExecuteDeleteAsync(cancellationToken);
        var migrator = db.GetService<IMigrator>();
        try
        {
            await migrator.MigrateAsync("20261117103652_AddApplicationStats", cancellationToken);
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO [stats] ([CapturedAtUtc], [LeaveAllocationCount], [LeaveRequestCount], [LeaveTypeCount]) VALUES (SYSUTCDATETIME(), 3, 9, 2)",
                cancellationToken);
            await migrator.MigrateAsync(cancellationToken: cancellationToken);

            var legacy = await db.Stats.AsNoTracking().SingleAsync(cancellationToken);
            Assert.Equal(9, legacy.LeaveRequestCount);
            Assert.Null(legacy.PendingLeaveRequestCount);
            Assert.Null(legacy.ApprovedLeaveRequestCount);
            Assert.Null(legacy.CancelledLeaveRequestCount);
            Assert.Null(legacy.PendingLeaveRequestAverageDays);
            Assert.Null(legacy.ApprovedLeaveRequestAverageDays);
            Assert.Null(legacy.CancelledLeaveRequestAverageDays);
            Assert.Null(legacy.PendingLeaveRequestPercentage);
            Assert.Null(legacy.ApprovedLeaveRequestPercentage);
            Assert.Null(legacy.CancelledLeaveRequestPercentage);
        }
        finally
        {
            await migrator.MigrateAsync(cancellationToken: CancellationToken.None);
        }
    }

    private LeaveRequest Request(bool? approved, bool cancelled, int days) => new()
    {
        RequestingEmployeeId = fixture.EmployeeId,
        LeaveTypeId = 1,
        // Les heures diffèrent pour vérifier le comptage en jours calendaires inclusifs.
        StartDate = new DateTime(2030, 1, 1, 18, 0, 0),
        EndDate = new DateTime(2030, 1, days, 8, 0, 0),
        DateRequested = DateTime.UtcNow,
        Approved = approved,
        Cancelled = cancelled
    };
}

// Fixture distinct : vider les types ne doit pas modifier les données des autres scénarios.
public sealed class EmptyApplicationStatsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Empty_tables_produce_zero_counts_and_percentages_and_undefined_average_durations()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HrDatabaseContext>();
        await db.LeaveRequests.ExecuteDeleteAsync(cancellationToken);
        await db.LeaveAllocations.ExecuteDeleteAsync(cancellationToken);
        await db.LeaveTypes.ExecuteDeleteAsync(cancellationToken);
        var empty = await scope.ServiceProvider.GetRequiredService<IApplicationStatsRepository>().CaptureAsync(cancellationToken);
        Assert.Equal(0, empty.LeaveAllocationCount);
        Assert.Equal(0, empty.LeaveRequestCount);
        Assert.Equal(0, empty.LeaveTypeCount);
        Assert.Equal(0, empty.PendingLeaveRequestCount);
        Assert.Equal(0, empty.ApprovedLeaveRequestCount);
        Assert.Equal(0, empty.CancelledLeaveRequestCount);
        Assert.Null(empty.PendingLeaveRequestAverageDays);
        Assert.Null(empty.ApprovedLeaveRequestAverageDays);
        Assert.Null(empty.CancelledLeaveRequestAverageDays);
        Assert.Equal(0m, empty.PendingLeaveRequestPercentage);
        Assert.Equal(0m, empty.ApprovedLeaveRequestPercentage);
        Assert.Equal(0m, empty.CancelledLeaveRequestPercentage);
        Assert.True(await db.Stats.AnyAsync(stat => stat.Id == empty.Id, cancellationToken));
    }
}
