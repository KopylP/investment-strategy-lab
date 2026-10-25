namespace InvestmentLab.BuildingBlocks.Scheduling;

/// <summary>Background work run on a schedule. Resolved in its own DI scope for every run.</summary>
public interface IScheduledJob
{
    Task RunAsync(CancellationToken cancellationToken);
}
