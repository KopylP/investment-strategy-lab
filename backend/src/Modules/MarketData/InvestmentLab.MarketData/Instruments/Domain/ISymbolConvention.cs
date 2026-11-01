namespace InvestmentLab.MarketData.Instruments.Domain;

public interface ISymbolConvention
{
    IReadOnlyList<string> CandidatesFor(string ticker, string currency);

    string ExchangeOf(string symbol);

    bool IsListingOf(string symbol, string ticker);
}
