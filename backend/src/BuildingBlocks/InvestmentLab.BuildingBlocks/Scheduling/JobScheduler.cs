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
    ILogger<JobScheduler> logger) : BackgroundService
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

            try
            {
                await Task.Delay(next - clock.GetUtcNow(), clock, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            await RunOnceAsync(job, stoppingToken);
        }
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
