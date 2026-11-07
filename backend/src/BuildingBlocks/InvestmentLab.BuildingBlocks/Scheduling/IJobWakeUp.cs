namespace InvestmentLab.BuildingBlocks.Scheduling;

public interface IJobWakeUp
{
    void Wake<TJob>() where TJob : IScheduledJob;
}
