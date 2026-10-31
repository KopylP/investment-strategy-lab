namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record AllocationRow(
    Guid? InstrumentId,
    AllocationRole Role,
    decimal ValueBefore,
    decimal ValueAfter,
    decimal WeightBefore,
    decimal WeightAfter,
    decimal? TargetWeight);
