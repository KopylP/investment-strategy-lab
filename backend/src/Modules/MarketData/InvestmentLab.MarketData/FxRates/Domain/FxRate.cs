namespace InvestmentLab.MarketData.FxRates.Domain;

public enum FxRateSource
{
    /// <summary>Narodowy Bank Polski, table A (average rates) — the rate Polish tax law uses.</summary>
    Nbp,
    Ecb,
}

/// <summary>How many <see cref="Quote"/> units one <see cref="Base"/> unit cost on <see cref="EffectiveDate"/>.</summary>
public sealed class FxRate
{
    private FxRate() { }

    public FxRateSource Source { get; private set; }
    public Currency Base { get; private set; } = Currency.EUR;
    public Currency Quote { get; private set; } = Currency.PLN;
    public DateOnly EffectiveDate { get; private set; }
    public decimal Rate { get; private set; }

    /// <summary>Published table, e.g. <c>085/A/NBP/2026</c>; tax returns cite it.</summary>
    public string? TableNumber { get; private set; }

    public static FxRate Of(FxRateSource source, Currency @base, Currency quote, DateOnly effectiveDate, decimal rate, string? tableNumber) =>
        rate > 0m
            ? new() { Source = source, Base = @base, Quote = quote, EffectiveDate = effectiveDate, Rate = rate, TableNumber = tableNumber }
            : throw new ArgumentOutOfRangeException(nameof(rate), rate, "FX rate must be positive.");
}
