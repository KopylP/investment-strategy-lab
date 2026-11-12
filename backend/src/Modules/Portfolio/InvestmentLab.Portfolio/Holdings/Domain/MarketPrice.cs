namespace InvestmentLab.Portfolio.Holdings.Domain;

public sealed record MarketPrice(DateOnly Date, decimal Quoted, Currency QuoteCurrency, decimal InAccountCurrency, bool IsStale)
{
    public static MarketPrice Of(DateOnly date, decimal quoted, Currency quoteCurrency, decimal rateToAccountCurrency, bool isStale) =>
        new(date, quoted, quoteCurrency, quoted * rateToAccountCurrency, isStale);
}
