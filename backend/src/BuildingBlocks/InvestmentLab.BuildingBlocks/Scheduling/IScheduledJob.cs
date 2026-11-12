namespace InvestmentLab.BuildingBlocks.Scheduling;

public interface IScheduledJob
{
    static abstract IJobSchedule Schedule { get; }

    static virtual bool RunAtStartup => false;

    Task RunAsync(CancellationToken cancellationToken);
}
