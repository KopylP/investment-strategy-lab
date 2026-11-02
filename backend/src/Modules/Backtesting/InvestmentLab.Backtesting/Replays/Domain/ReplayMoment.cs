namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayMoment(DateOnly Day, ReplayBook Book, IReadOnlyDictionary<Guid, decimal> Prices, double Growth);
