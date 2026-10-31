namespace InvestmentLab.MarketData.FxRates.Domain;

public static class FxRateErrors
{
    public static Error NotFound(Currency @base, Currency quote, DateOnly transactionDate) =>
        Error.NotFound("fx_rate.not_found",
            $"No {@base}/{quote} rate within {FxRateResolver.MaxAgeDays} days before {transactionDate:yyyy-MM-dd}.");
}
