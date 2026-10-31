namespace InvestmentLab.MarketData.Instruments.Application;

public interface ISymbolLookup
{
    Task<IReadOnlyList<SymbolMatch?>> LookupAsync(IReadOnlyList<SymbolQuery> queries, CancellationToken cancellationToken);
}
