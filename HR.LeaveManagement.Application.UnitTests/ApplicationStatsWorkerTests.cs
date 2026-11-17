using System.Collections.Concurrent;
using System.Threading.Channels;
using HR.LeaveManagement.Application.Contracts.Persistence;
using HR.LeaveManagement.Domain;
using HR.LeaveManagement.Infrastructure;
using HR.LeaveManagement.Infrastructure.Options;
using HR.LeaveManagement.Infrastructure.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HR.LeaveManagement.Application.UnitTests;

public class ApplicationStatsWorkerTests
{
    [Fact]
    public async Task Worker_waits_for_configured_interval_and_uses_a_new_scope_for_each_capture()
    {
        var clock = new ManualTimeProvider();
        var state = new CaptureState();
        var logger = new WorkerLogger();
        using var provider = BuildProvider(clock, state, logger);
        var worker = Assert.IsType<ApplicationStatsWorker>(Assert.Single(provider.GetServices<IHostedService>()));

        await worker.StartAsync(TestContext.Current.CancellationToken);
        var timer = await clock.Created.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(TimeSpan.FromSeconds(17), timer.Period);
        Assert.Equal(TimeSpan.FromSeconds(17), timer.DueTime);
        Assert.Equal(0, state.Attempts);

        timer.Tick();
        var first = await state.Captures.Reader.ReadAsync(TestContext.Current.CancellationToken);
        await first.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timer.Tick();
        var second = await state.Captures.Reader.ReadAsync(TestContext.Current.CancellationToken);
        await second.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);

        Assert.NotSame(first, second);
        Assert.Equal(2, state.Attempts);
        Assert.Equal(2, logger.Entries.Count(entry => entry.Message.Contains("Statistiques enregistrées")));
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("LeaveAllocation=7, LeaveRequest=4, LeaveType=2"));
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("En attente=2") &&
            entry.Message.Contains("Approuvées=1") && entry.Message.Contains("Annulées=1") &&
            entry.Message.Contains("durée moyenne="));
        Assert.DoesNotContain(logger.Entries, entry => entry.Level >= LogLevel.Error);
        Assert.True(timer.IsDisposed);
    }

    [Fact]
    public async Task Failure_is_logged_and_next_tick_can_capture_successfully()
    {
        var clock = new ManualTimeProvider();
        var state = new CaptureState { FailFirst = true };
        var logger = new WorkerLogger();
        using var provider = BuildProvider(clock, state, logger);
        var worker = Assert.Single(provider.GetServices<IHostedService>());
        await worker.StartAsync(TestContext.Current.CancellationToken);
        var timer = await clock.Created.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        timer.Tick();
        var failed = await state.Captures.Reader.ReadAsync(TestContext.Current.CancellationToken);
        await failed.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timer.Tick();
        var successful = await state.Captures.Reader.ReadAsync(TestContext.Current.CancellationToken);
        await successful.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, state.Attempts);
        var error = Assert.Single(logger.Entries, entry => entry.Level == LogLevel.Error);
        Assert.IsType<InvalidOperationException>(error.Exception);
        Assert.Single(logger.Entries, entry => entry.Message.Contains("Statistiques enregistrées"));
    }

    [Fact]
    public async Task Stopping_worker_cancels_the_current_capture_without_reporting_an_error_or_overlapping_work()
    {
        var clock = new ManualTimeProvider();
        var state = new CaptureState { WaitForCancellation = true };
        var logger = new WorkerLogger();
        using var provider = BuildProvider(clock, state, logger);
        var worker = Assert.Single(provider.GetServices<IHostedService>());
        await worker.StartAsync(TestContext.Current.CancellationToken);
        var timer = await clock.Created.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        timer.Tick();
        var capture = await state.Captures.Reader.ReadAsync(TestContext.Current.CancellationToken);

        timer.Tick();
        timer.Tick();
        Assert.Equal(1, state.Attempts);
        await worker.StopAsync(TestContext.Current.CancellationToken);
        await capture.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        Assert.True(capture.CancellationToken.IsCancellationRequested);
        Assert.DoesNotContain(logger.Entries, entry => entry.Level >= LogLevel.Error);
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("Statistiques enregistrées"));
    }

    [Fact]
    public async Task Disabled_worker_does_not_start_a_timer_or_resolve_the_repository()
    {
        var clock = new ManualTimeProvider();
        var state = new CaptureState();
        using var provider = BuildProvider(clock, state, new WorkerLogger(), enabled: false);
        var worker = Assert.IsType<ApplicationStatsWorker>(Assert.Single(provider.GetServices<IHostedService>()));
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);
        Assert.False(clock.Created.Task.IsCompleted);
        Assert.Equal(0, state.Attempts);
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-00:01:00")]
    [InlineData("50.00:00:00")]
    public void Invalid_interval_is_rejected(string interval)
    {
        using var provider = BuildProvider(new ManualTimeProvider(), new CaptureState(), new WorkerLogger(), interval: interval);
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<StatsWorkerOptions>>().Value);
    }

    [Fact]
    public void Default_interval_is_three_minutes()
    {
        using var provider = new ServiceCollection().AddLogging().AddInfrastructureServices(
            new ConfigurationBuilder().Build()).BuildServiceProvider();
        Assert.Equal(TimeSpan.FromMinutes(3), provider.GetRequiredService<IOptions<StatsWorkerOptions>>().Value.Interval);
    }

    private static ServiceProvider BuildProvider(ManualTimeProvider clock, CaptureState state, WorkerLogger logger,
        bool enabled = true, string interval = "00:00:17")
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["StatsWorker:Enabled"] = enabled.ToString(), ["StatsWorker:Interval"] = interval
        }).Build();
        var services = new ServiceCollection().AddLogging();
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton<ILogger<ApplicationStatsWorker>>(logger);
        services.AddSingleton(state);
        services.AddScoped<IApplicationStatsRepository, ScopedRepository>();
        services.AddInfrastructureServices(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private sealed class CaptureState
    {
        public int Attempts;
        public bool FailFirst { get; init; }
        public bool WaitForCancellation { get; init; }
        public Channel<ScopedRepository> Captures { get; } = Channel.CreateUnbounded<ScopedRepository>();
    }

    private sealed class ScopedRepository(CaptureState state) : IApplicationStatsRepository, IAsyncDisposable
    {
        public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken CancellationToken { get; private set; }
        public async Task<ApplicationStat> CaptureAsync(CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            var attempt = Interlocked.Increment(ref state.Attempts);
            state.Captures.Writer.TryWrite(this);
            if (state.WaitForCancellation) await Task.Delay(Timeout.Infinite, cancellationToken);
            if (state.FailFirst && attempt == 1) throw new InvalidOperationException("SQL temporarily unavailable");
            return new ApplicationStat
            {
                Id = attempt, CapturedAtUtc = DateTime.UtcNow,
                LeaveAllocationCount = 7, LeaveRequestCount = 4, LeaveTypeCount = 2,
                PendingLeaveRequestCount = 2, ApprovedLeaveRequestCount = 1, CancelledLeaveRequestCount = 1,
                PendingLeaveRequestAverageDays = 1.5m, ApprovedLeaveRequestAverageDays = 3m, CancelledLeaveRequestAverageDays = 2m,
                PendingLeaveRequestPercentage = 50m, ApprovedLeaveRequestPercentage = 25m, CancelledLeaveRequestPercentage = 25m
            };
        }
        public ValueTask DisposeAsync() { Disposed.TrySetResult(); return ValueTask.CompletedTask; }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        public TaskCompletionSource<ManualTimer> Created { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            var timer = new ManualTimer(callback, state, dueTime, period);
            Created.TrySetResult(timer);
            return timer;
        }
    }

    private sealed class ManualTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) : ITimer
    {
        public TimeSpan DueTime { get; } = dueTime;
        public TimeSpan Period { get; } = period;
        public bool IsDisposed { get; private set; }
        public void Tick() { if (!IsDisposed) callback(state); }
        public bool Change(TimeSpan dueTime, TimeSpan period) => !IsDisposed;
        public void Dispose() => IsDisposed = true;
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }

    private sealed class WorkerLogger : ILogger<ApplicationStatsWorker>
    {
        public ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Enqueue((logLevel, formatter(state, exception), exception));
    }
}
