namespace InvestmentLab.BuildingBlocks.Scheduling;

public interface IScheduledJob
{
    Task RunAsync(CancellationToken cancellationToken);
}
