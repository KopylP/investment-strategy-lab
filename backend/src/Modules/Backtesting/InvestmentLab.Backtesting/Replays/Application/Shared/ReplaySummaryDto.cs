using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record ReplaySummaryDto(
    double TotalReturn,
    double AnnualReturn,
    double Volatility,
    double Sharpe,
    double MaxDrawdown,
    double? AnnualReturnAfterTax)
{
    public static ReplaySummaryDto From(ReplaySummary replaySummary) => new(
        TotalReturn: replaySummary.TotalReturn,
        AnnualReturn: replaySummary.AnnualReturn,
        Volatility: replaySummary.Volatility,
        Sharpe: replaySummary.Sharpe,
        MaxDrawdown: replaySummary.MaxDrawdown,
        AnnualReturnAfterTax: replaySummary.AnnualReturnAfterTax);
}
