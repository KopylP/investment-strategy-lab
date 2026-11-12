namespace InvestmentLab.Performance.Valuation.Domain;

public static class ValuationErrors
{
    public static Error NoExchangeRate(string currency, DateOnly day) =>
        Error.Conflict(
            "valuation.no_exchange_rate",
            $"Cash in {currency} is held on {day:yyyy-MM-dd}, but no exchange rate of {currency} is known on that day or before it.",
            new { currency, day });
}
