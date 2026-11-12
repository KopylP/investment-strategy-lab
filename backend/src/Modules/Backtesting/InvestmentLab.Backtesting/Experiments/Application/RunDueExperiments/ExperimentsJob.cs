using InvestmentLab.Backtesting.Experiments.Application.FailExperiment;
using InvestmentLab.Backtesting.Experiments.Application.RunExperiment;
using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.BuildingBlocks.Scheduling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Backtesting.Experiments.Application.RunDueExperiments;

public sealed class ExperimentsJob(IServiceScopeFactory scopes, ILogger<ExperimentsJob> logger) : IScheduledJob
{
    public static IJobSchedule Schedule { get; } = new IntervalSchedule(TimeSpan.FromSeconds(15));

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (await Next(cancellationToken) is { } id)
            await RunOne(id, cancellationToken);
    }

    private async Task<Guid?> Next(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var experiments = scope.ServiceProvider.GetRequiredService<IExperimentRepository>();

        return await experiments.NextAsync(cancellationToken);
    }

    private async Task RunOne(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<RunExperimentHandler>();

            await handler.Handle(id, cancellationToken);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "Experiment {Id} failed", id);
            await Fail(id, cancellationToken);
        }
    }

    private async Task Fail(Guid id, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<FailExperimentHandler>();

        await handler.Handle(id, cancellationToken);
    }
}
