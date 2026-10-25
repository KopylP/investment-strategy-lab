namespace InvestmentLab.MarketData.Quotes.Domain;

[Flags]
public enum QualityFlags
{
    None = 0,

    /// <summary>The latest quote is older than allowed; set when reading, never stored.</summary>
    Stale = 1,

    /// <summary>More than <see cref="QuoteQualityInspector.MaxMissingBusinessDays"/> business days without a quote before this one.</summary>
    Gap = 2,

    /// <summary>Moved more than <see cref="QuoteQualityInspector.SpikeThreshold"/> against the previous quote.</summary>
    Spike = 4,

    /// <summary>Fixed by hand; ingestion leaves it alone.</summary>
    Corrected = 8,
}
