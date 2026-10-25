namespace InvestmentLab.MarketData.Instruments.Domain;

public enum InstrumentKind
{
    Etf,
    Index,
    FxPair,

    /// <summary>A single share, e.g. bought outside the strategies and brought in by a statement import.</summary>
    Stock,
}
