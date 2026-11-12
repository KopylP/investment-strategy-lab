using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchSummaryDto(
    double TotalReturn,
    double AnnualReturn,
    double Volatility,
    double Sharpe,
    double MaxDrawdown,
    double? AnnualReturnAfterTax)
{
    public static ResearchSummaryDto From(ReplaySummary replaySummary) => new(
        TotalReturn: replaySummary.TotalReturn,
        AnnualReturn: replaySummary.AnnualReturn,
        Volatility: replaySummary.Volatility,
        Sharpe: replaySummary.Sharpe,
        MaxDrawdown: replaySummary.MaxDrawdown,
        AnnualReturnAfterTax: replaySummary.AnnualReturnAfterTax);
}
