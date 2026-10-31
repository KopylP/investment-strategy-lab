namespace InvestmentLab.MarketData.FxRates.Domain;

public static class FxRateResolver
{
    public const int MaxAgeDays = 10;

    public static Result<FxRate> Resolve(IEnumerable<FxRate> rates, Currency @base, Currency quote, DateOnly transactionDate)
    {
        if (@base == quote)
            return FxRate.Of(FxRateSource.Nbp, @base, quote, transactionDate.AddDays(-1), 1m, tableNumber: null);

        var candidate = rates
            .Where(rate => rate.Base == @base && rate.Quote == quote && rate.EffectiveDate < transactionDate)
            .MaxBy(rate => rate.EffectiveDate);

        return candidate is not null && transactionDate.DayNumber - candidate.EffectiveDate.DayNumber <= MaxAgeDays
            ? candidate
            : FxRateErrors.NotFound(@base, quote, transactionDate);
    }
}
