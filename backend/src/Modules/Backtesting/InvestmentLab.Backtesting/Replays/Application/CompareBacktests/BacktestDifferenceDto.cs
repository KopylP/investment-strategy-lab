using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.CompareBacktests;

public sealed record BacktestDifferenceDto(
    double AnnualReturn,
    double? AnnualReturnAfterTax,
    double Volatility,
    double Sharpe,
    double MaxDrawdown)
{
    public static BacktestDifferenceDto From(BacktestDifference backtestDifference) => new(
        AnnualReturn: backtestDifference.AnnualReturn,
        AnnualReturnAfterTax: backtestDifference.AnnualReturnAfterTax,
        Volatility: backtestDifference.Volatility,
        Sharpe: backtestDifference.Sharpe,
        MaxDrawdown: backtestDifference.MaxDrawdown);
}
