namespace InvestmentLab.MarketData.Contracts.FxRates;

/// <summary>Exchange rates for taxes and valuation.</summary>
public interface IFxRates
{
    /// <summary>
    /// The NBP rate that applies to a transaction on <paramref name="transactionDate"/>: the last table strictly before it,
    /// at most ten days old. Null when no such rate is stored.
    /// </summary>
    Task<FxRateDto?> GetFxRate(string baseCurrency, string quoteCurrency, DateOnly transactionDate, CancellationToken cancellationToken);

    /// <summary>Every stored NBP table for the pair from <paramref name="from"/> to <paramref name="to"/>, oldest first (valuation over time).</summary>
    Task<IReadOnlyList<FxRateDto>> GetFxRateHistory(string baseCurrency, string quoteCurrency, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}

public sealed record FxRateDto(string Source, string Base, string Quote, DateOnly EffectiveDate, decimal Rate, string? TableNumber);
