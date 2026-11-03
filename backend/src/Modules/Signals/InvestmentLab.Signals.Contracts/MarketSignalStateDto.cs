namespace InvestmentLab.Signals.Contracts;

public sealed record MarketSignalStateDto(Guid SignalId, IReadOnlyList<Guid> ActiveFor);
