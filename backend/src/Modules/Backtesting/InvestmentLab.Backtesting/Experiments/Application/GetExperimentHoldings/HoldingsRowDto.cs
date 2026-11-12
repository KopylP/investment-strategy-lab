using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperimentHoldings;

public sealed record HoldingsRowDto(
    DateOnly Date,
    bool? MarketFavorable,
    IReadOnlyList<HeldShareDto> Positions,
    double CashShare)
{
    public static HoldingsRowDto From(HoldingsRow holdingsRow) => new(
        Date: holdingsRow.Date,
        MarketFavorable: holdingsRow.MarketFavorable,
        Positions: [.. holdingsRow.Positions.Select(HeldShareDto.From)],
        CashShare: holdingsRow.CashShare);
}
