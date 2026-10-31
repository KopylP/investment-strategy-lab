namespace InvestmentLab.MarketData.Instruments.Application.Shared;

public interface ISymbolLookup
{
    Task<IReadOnlyList<SymbolMatch?>> LookupAsync(IReadOnlyList<SymbolQuery> queries, CancellationToken cancellationToken);
}
