using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.BuildingBlocks.Scheduling;

public sealed record ScheduledJobRegistration(Type JobType, DailySchedule Schedule);

public static class SchedulingRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddScheduledJob<TJob>(DailySchedule schedule)
            where TJob : class, IScheduledJob
        {
            services.TryAddScoped<TJob>();
            services.AddSingleton(new ScheduledJobRegistration(typeof(TJob), schedule));
            return services;
        }
    }
}

/// <summary>Runs every registered job at its next occurrence; a failed run is logged and waits for the next one.</summary>
internal sealed class JobScheduler(
    IEnumerable<ScheduledJobRegistration> jobs,
    IServiceScopeFactory scopes,
    TimeProvider clock,
    ILogger<JobScheduler> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(jobs.Select(job => RunForeverAsync(job, stoppingToken)));

    private async Task RunForeverAsync(ScheduledJobRegistration job, CancellationToken stoppingToken)
    {
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
