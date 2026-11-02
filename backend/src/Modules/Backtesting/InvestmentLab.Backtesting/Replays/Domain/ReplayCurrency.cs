namespace InvestmentLab.Backtesting.Replays.Domain;

public static class ReplayCurrency
{
    public static string Of(IReadOnlyList<string> tradingCurrenciesOfUniverse)
    {
        if (tradingCurrenciesOfUniverse.Count == 0)
            throw new ArgumentException("A strategy without funds has no currency to replay in.", nameof(tradingCurrenciesOfUniverse));

        var byCurrency = tradingCurrenciesOfUniverse.GroupBy(currency => currency.ToUpperInvariant());

        return byCurrency.OrderByDescending(funds => funds.Count()).First().Key;
    }
}
