namespace InvestmentLab.MarketData.Quotes.Domain;

[Flags]
public enum QualityFlags
{
    None = 0,

    Stale = 1,

    Gap = 2,

    Spike = 4,

    Corrected = 8,
}
