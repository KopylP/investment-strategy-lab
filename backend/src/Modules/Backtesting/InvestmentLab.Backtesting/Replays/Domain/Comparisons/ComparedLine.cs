namespace InvestmentLab.Backtesting.Replays.Domain.Comparisons;

public sealed record ComparedLine(string Code, string Label, IReadOnlyList<GrowthDay> Growth, Guid HeldAs);
