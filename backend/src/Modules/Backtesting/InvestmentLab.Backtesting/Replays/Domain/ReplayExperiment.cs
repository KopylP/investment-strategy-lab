namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayExperiment(DateOnly Start, DateOnly End, decimal InitialCapital, TradingCosts Costs, bool RecordJournal = true);
