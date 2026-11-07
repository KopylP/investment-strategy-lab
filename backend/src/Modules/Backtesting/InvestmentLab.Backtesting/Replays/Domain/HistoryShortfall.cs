namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record HistoryShortfall(DateOnly EarliestStart, HistoryLimit Limit);
