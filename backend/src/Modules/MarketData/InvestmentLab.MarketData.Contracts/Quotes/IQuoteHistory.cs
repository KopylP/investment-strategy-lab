namespace InvestmentLab.MarketData.Contracts.Quotes;

/// <summary>Daily quotes kept by MarketData, for strategies, reports and valuation.</summary>
public interface IQuoteHistory
{
    /// <summary>Quotes of each instrument from <paramref name="from"/> to <paramref name="to"/> inclusive, oldest first.</summary>
    Task<IReadOnlyList<QuoteSeriesDto>> GetQuoteHistory(
        IReadOnlyCollection<Guid> instrumentIds, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    /// <summary>The last quote on or before <paramref name="asOf"/> per instrument; instruments without quotes are left out.</summary>
    Task<IReadOnlyList<LatestPriceDto>> GetLatestPrices(
        IReadOnlyCollection<Guid> instrumentIds, DateOnly asOf, CancellationToken cancellationToken);
}

public sealed record QuoteSeriesDto(Guid InstrumentId, IReadOnlyList<QuotePointDto> Points);

/// <param name="Flags">Quality flags: Gap, Spike, Corrected.</param>
public sealed record QuotePointDto(DateOnly Date, decimal Close, decimal AdjustedClose, IReadOnlyList<string> Flags);

/// <param name="IsStale">The quote is older than five business days before <c>asOf</c>.</param>
public sealed record LatestPriceDto(Guid InstrumentId, DateOnly Date, decimal Close, decimal AdjustedClose, string Currency, bool IsStale);
