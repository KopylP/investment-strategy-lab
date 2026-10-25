using System.Net.Http.Json;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Application;

namespace InvestmentLab.MarketData.Quotes.Infrastructure.YFinance;

/// <summary>Anti-corruption layer over the strategy engine's <c>POST /v1/prices</c> (Yahoo Finance).</summary>
internal sealed class YFinanceQuoteProvider(HttpClient http) : IQuoteProvider
{
    private sealed record PricesRequest(IReadOnlyList<string> Symbols, DateOnly Start);

    private sealed record QuoteSeries(string Symbol, List<DateOnly> Dates, List<double?> Close, List<double?> AdjustedClose);

    private sealed record PricesResponse(List<QuoteSeries> Series, List<string> Missing);

    public QuoteProvider Provider => QuoteProvider.YFinance;

    public async Task<FetchedQuotes> FetchAsync(IReadOnlyList<string> symbols, DateOnly from, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync("/v1/prices", new PricesRequest(symbols, from), cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PricesResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Empty /v1/prices response.");

        return new FetchedQuotes(
            [.. body.Series.Select(series => new FetchedSeries(series.Symbol,
                [
                    .. series.Dates.Select((date, i) =>
                        new FetchedQuote(date, ToPrice(series.Close[i]), ToPrice(series.AdjustedClose[i])))
                ]))
            ],
            body.Missing);
    }

    // Yahoo prices come as floats; 8 decimals is what the database keeps.
    private static decimal? ToPrice(double? value) =>
        value is { } price && double.IsFinite(price) && price > 0 ? decimal.Round((decimal)price, 8) : null;
}
