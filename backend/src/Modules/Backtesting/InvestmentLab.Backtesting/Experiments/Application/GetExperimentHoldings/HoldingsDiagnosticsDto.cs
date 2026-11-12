using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperimentHoldings;

public sealed record HoldingsDiagnosticsDto(
    double? UnfavorableShare,
    double MeanInvestedShare,
    double MeanFunds,
    double TradesPerYear)
{
    public static HoldingsDiagnosticsDto From(HoldingsDiagnostics holdingsDiagnostics) => new(
        UnfavorableShare: holdingsDiagnostics.UnfavorableShare,
        MeanInvestedShare: holdingsDiagnostics.MeanInvestedShare,
        MeanFunds: holdingsDiagnostics.MeanFunds,
        TradesPerYear: holdingsDiagnostics.TradesPerYear);
}
