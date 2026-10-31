namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record InstrumentScore(Guid InstrumentId, int? Rank, decimal? Momentum, string Reason);
