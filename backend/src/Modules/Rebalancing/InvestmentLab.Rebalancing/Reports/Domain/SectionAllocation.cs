namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record SectionAllocation(
    Guid? InstrumentId, string Role, decimal ValueBefore, decimal ValueAfter, decimal WeightBefore, decimal WeightAfter, decimal? TargetWeight);
