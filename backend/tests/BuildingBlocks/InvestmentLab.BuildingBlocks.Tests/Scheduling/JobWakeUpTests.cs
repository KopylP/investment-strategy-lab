using InvestmentLab.BuildingBlocks.Scheduling;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvestmentLab.BuildingBlocks.Tests.Scheduling;

public sealed class JobWakeUpTests
{
    private static readonly TimeSpan Hourly = TimeSpan.FromHours(1);
    private static readonly TimeSpan Promptly = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task A_woken_job_runs_without_waiting_for_its_schedule()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var services = new ServiceCollection();
        services.AddSingleton<RunCounter>();
        services.AddScheduledJob<CountedJob>(new IntervalSchedule(Hourly));
        await using var provider = services.BuildServiceProvider();
        var scheduler = SchedulerOf(provider);

        await scheduler.StartAsync(cancellationToken);
        provider.GetRequiredService<IJobWakeUp>().Wake<CountedJob>();
        var ran = await provider.GetRequiredService<RunCounter>().FirstRun.Task.WaitAsync(Promptly, cancellationToken);
        await scheduler.StopAsync(cancellationToken);

        ran.ShouldBeTrue();
    }

    private static JobScheduler SchedulerOf(ServiceProvider provider) => new(
        jobs: provider.GetServices<ScheduledJobRegistration>(),
        scopes: provider.GetRequiredService<IServiceScopeFactory>(),
        configuration: new ConfigurationBuilder().Build(),
        clock: TimeProvider.System,
        logger: NullLogger<JobScheduler>.Instance,
        wakeUps: provider.GetRequiredService<JobWakeUps>());

    private sealed class RunCounter
    {
        public TaskCompletionSource<bool> FirstRun { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class CountedJob(RunCounter counter) : IScheduledJob
    {
        public Task RunAsync(CancellationToken cancellationToken)
        {
            counter.FirstRun.TrySetResult(true);

            return Task.CompletedTask;
        }
    }
}
