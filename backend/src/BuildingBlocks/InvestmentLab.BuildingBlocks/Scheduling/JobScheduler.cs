using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.BuildingBlocks.Scheduling;

internal sealed class JobScheduler(
    IEnumerable<ScheduledJobRegistration> jobs,
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<JobScheduler> logger,
    JobWakeUps wakeUps) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(jobs.Select(job => RunForeverAsync(job, stoppingToken)));

    private async Task RunForeverAsync(ScheduledJobRegistration job, CancellationToken stoppingToken)
    {
        if (job.RunAtStartup && configuration.GetValue("Scheduling:RunAtStartup", true))
        {
            logger.LogInformation("Job {Job} runs at startup", job.JobType.Name);
            await RunOnceAsync(job, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var next = job.Schedule.NextOccurrenceAfter(clock.GetUtcNow());
            logger.LogInformation("Job {Job} next runs at {NextRun:O}", job.JobType.Name, next);

            await WaitForTurnAsync(job, next, stoppingToken);
            if (stoppingToken.IsCancellationRequested)
                return;

            await RunOnceAsync(job, stoppingToken);
        }
    }

    private async Task WaitForTurnAsync(ScheduledJobRegistration job, DateTimeOffset next, CancellationToken stoppingToken)
    {
        using var turn = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var untilNext = next - clock.GetUtcNow();
        var scheduled = Task.Delay(untilNext > TimeSpan.Zero ? untilNext : TimeSpan.Zero, clock, turn.Token);
        var wokenUp = wakeUps.WaitAsync(job.JobType, turn.Token);

        await Task.WhenAny(scheduled, wokenUp);
        await turn.CancelAsync();
        await Task.WhenAll(scheduled, wokenUp).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
    }

    private async Task RunOnceAsync(ScheduledJobRegistration job, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            await ((IScheduledJob)scope.ServiceProvider.GetRequiredService(job.JobType)).RunAsync(stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Job {Job} failed", job.JobType.Name);
        }
    }
}
