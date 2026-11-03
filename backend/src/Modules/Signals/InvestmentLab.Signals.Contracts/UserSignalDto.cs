namespace InvestmentLab.Signals.Contracts;

public sealed record UserSignalDto(
    Guid Id,
    Guid OwnerId,
    string Name,
    string Target,
    Guid? InstrumentId,
    string? GroupName,
    Guid? AccountId,
    string Metric,
    decimal Threshold,
    bool IsMarketSignal);
