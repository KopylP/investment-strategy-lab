using System.Net.Http.Json;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Application.Shared;

namespace InvestmentLab.MarketData.Quotes.Infrastructure.YFinance;

internal sealed class YFinanceQuoteProvider(HttpClient http) : IQuoteProvider
{
    private sealed record PricesRequest(IReadOnlyList<string> Symbols, DateOnly Start);

    private sealed record QuoteSeries(
        string Symbol,
        List<DateOnly> Dates,
        List<double?> Open,
        List<double?> High,
        List<double?> Low,
        List<double?> Close,
        List<double?> AdjustedClose);

    private sealed record PricesResponse(List<QuoteSeries> Series, List<string> Missing);

    public QuoteProvider Provider => QuoteProvider.YFinance;

    public async Task<FetchedQuotes> FetchAsync(IReadOnlyList<string> symbols, DateOnly from, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync("/v1/prices", new PricesRequest(symbols, from), cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PricesResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Empty /v1/prices response.");

        var series = body.Series.Select(ToFetchedSeries).ToList();

        return new FetchedQuotes(series, body.Missing);
    }

    private static FetchedSeries ToFetchedSeries(QuoteSeries series)
    {
        var quotes = series.Dates.Select((date, i) => new FetchedQuote(
            Date: date,
            Open: ToPrice(series.Open[i]),
            High: ToPrice(series.High[i]),
            Low: ToPrice(series.Low[i]),
            Close: ToPrice(series.Close[i]),
            AdjustedClose: ToPrice(series.AdjustedClose[i])));

        return new FetchedSeries(series.Symbol, [.. quotes]);
    }

    private static decimal? ToPrice(double? value) =>
        value is { } price && double.IsFinite(price) && price > 0 ? decimal.Round((decimal)price, 8) : null;
}
