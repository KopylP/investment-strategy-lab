namespace InvestmentLab.Backtesting.Replays.Domain.Comparisons;

public sealed record ReplayedHistory(PriceTable Prices, int FirstDay, ReplaySetup Setup, IReadOnlyList<GrowthDay> StrategyGrowth)
{
    public double RiskFreeRate
    {
        get
        {
            var growth = CashGrowth;

            return growth.Count > 1 ? ReplaySeries.AnnualReturnOf(growth) : 0d;
        }
    }

    public IReadOnlyList<GrowthDay> CashGrowth =>
        Setup.CashInstrumentId is { } cash ? Prices.GrowthOf(cash, FirstDay) : [];

    public double ReturnOn(Guid? instrumentId, int dayIndex) =>
        instrumentId is { } id && Prices.ReturnOn(id, dayIndex) is { } dayReturn ? dayReturn : 0d;
}
