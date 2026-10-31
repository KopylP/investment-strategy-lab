namespace InvestmentLab.SharedKernel;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
