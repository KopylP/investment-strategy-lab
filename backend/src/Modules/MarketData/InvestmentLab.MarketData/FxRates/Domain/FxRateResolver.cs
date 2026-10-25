namespace InvestmentLab.MarketData.FxRates.Domain;

/// <summary>
/// Belka rule (art. 11a PIT): convert at the NBP average rate of the last business day before the transaction.
/// Days without a table (weekends, Polish holidays) fall back to the previous table, but never further than
/// <see cref="MaxAgeDays"/> — an older rate means data is missing, not a long holiday.
/// </summary>
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

public static class FxRateErrors
{
    public static Error NotFound(Currency @base, Currency quote, DateOnly transactionDate) =>
        Error.NotFound("fx_rate.not_found",
            $"No {@base}/{quote} rate within {FxRateResolver.MaxAgeDays} days before {transactionDate:yyyy-MM-dd}.");
}
