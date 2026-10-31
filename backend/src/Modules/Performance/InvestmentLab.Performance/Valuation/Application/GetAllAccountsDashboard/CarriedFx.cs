using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Valuation.Application.GetAllAccountsDashboard;

public static class CarriedFx
{
    public static FxOn Over(FxOn fx, IReadOnlyCollection<string> currencies, IReadOnlyList<DateOnly> days)
    {
        var ratesOfCurrency = currencies.Distinct().ToDictionary(currency => currency, currency => RatesByDay(fx, currency, days));

        return (currency, day) => ratesOfCurrency.TryGetValue(currency, out var rates) ? rates.GetValueOrDefault(day) : null;
    }

    private static Dictionary<DateOnly, decimal?> RatesByDay(FxOn fx, string currency, IReadOnlyList<DateOnly> days)
    {
        var carried = days.Select(day => fx(currency, day)).FirstOrDefault(rate => rate is not null);
        var rates = new Dictionary<DateOnly, decimal?>();

        foreach (var day in days)
        {
            carried = fx(currency, day) ?? carried;
            rates[day] = carried;
        }

        return rates;
    }
}
