namespace InvestmentLab.Backtesting.Replays.Domain.Comparisons;

public sealed record ReplayedHistory(PriceTable Prices, int FirstDay, ReplaySetup Setup, IReadOnlyList<GrowthDay> StrategyGrowth)
{
    public double RiskFreeRate
    {
        get
        {
            if (Setup.CashInstrumentId is not { } cash)
                return 0d;

            var growth = Prices.GrowthOf(cash, FirstDay);

            return growth.Count > 1 ? ReplaySeries.AnnualReturnOf(growth) : 0d;
        }
    }

    public double ReturnOn(Guid? instrumentId, int dayIndex) =>
        instrumentId is { } id && Prices.ReturnOn(id, dayIndex) is { } dayReturn ? dayReturn : 0d;
}
