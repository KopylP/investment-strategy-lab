namespace InvestmentLab.BuildingBlocks.Scheduling;

public interface IJobSchedule
{
    DateTimeOffset NextOccurrenceAfter(DateTimeOffset instant);
}
