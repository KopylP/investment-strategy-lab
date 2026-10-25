using System.Net.Http.Json;
using InvestmentLab.MarketData.Instruments.Application;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.MarketData.Instruments.Infrastructure;

/// <summary>
/// Anti-corruption layer over the strategy engine's <c>POST /v1/symbols/lookup</c>. A lookup only helps the user, so when the
/// engine cannot be reached every query is simply not found.
/// </summary>
internal sealed class YFinanceSymbolLookup(HttpClient http, ILogger<YFinanceSymbolLookup> logger) : ISymbolLookup
{
    private sealed record LookupRequest(IReadOnlyList<SymbolQuery> Queries);

    private sealed record LookupResponse(List<SymbolMatch?> Results);

    public async Task<IReadOnlyList<SymbolMatch?>> LookupAsync(IReadOnlyList<SymbolQuery> queries, CancellationToken cancellationToken)
    {
        if (queries.Count == 0)
            return [];
        try
        {
            using var response = await http.PostAsJsonAsync("/v1/symbols/lookup", new LookupRequest(queries), cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<LookupResponse>(cancellationToken);
            if (body?.Results.Count == queries.Count)
                return body.Results;
            logger.LogWarning("Symbol lookup answered {Answered} of {Asked} queries", body?.Results.Count ?? 0, queries.Count);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Symbol lookup failed");
        }
        return [.. queries.Select(_ => (SymbolMatch?)null)];
    }
}
