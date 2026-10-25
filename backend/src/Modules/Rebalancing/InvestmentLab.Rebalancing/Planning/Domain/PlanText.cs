using System.Globalization;

namespace InvestmentLab.Rebalancing.Planning.Domain;

/// <summary>Formatting of reasons and notes, the same as the Python reports (UI language: Ukrainian).</summary>
internal static class PlanText
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>0.25 → "25%".</summary>
    public static string Percent(decimal fraction) =>
        decimal.Round(fraction * 100m, 0, MidpointRounding.ToEven).ToString("0", Invariant) + "%";

    /// <summary>0.29 → "+29.0%".</summary>
    public static string SignedPercent(decimal fraction)
    {
        var value = decimal.Round(fraction * 100m, 1, MidpointRounding.ToEven);
        return (value < 0m ? "-" : "+") + Math.Abs(value).ToString("0.0", Invariant) + "%";
    }

    /// <summary>500 EUR → "€500.00", 1234.5 PLN → "zł 1,234.50".</summary>
    public static string Money(decimal amount, Currency currency)
    {
        var prefix = currency.Code switch
        {
            "EUR" => "€",
            "USD" => "$",
            "GBP" => "£",
            "PLN" => "zł ",
            var code => code + " ",
        };
        var rounded = decimal.Round(Math.Abs(amount), 2, MidpointRounding.ToEven);
        return (amount < 0m ? "-" : "") + prefix + rounded.ToString("#,##0.00", Invariant);
    }
}
