using System.Net.Http.Json;
using InvestmentLab.MarketData.Instruments.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.MarketData.Instruments.Infrastructure;

internal sealed class YFinanceFundScreener(HttpClient http, ILogger<YFinanceFundScreener> logger) : IFundScreener
{
    public static readonly FundScreenOptions ScreenOptions = new(
        Exchanges: [
            new("GER", Message.Of("market_data.exchange.xetra")),
            new("LSE", Message.Of("market_data.exchange.london")),
            new("AMS", Message.Of("market_data.exchange.amsterdam")),
            new("PAR", Message.Of("market_data.exchange.paris")),
            new("MIL", Message.Of("market_data.exchange.milan")),
            new("EBS", Message.Of("market_data.exchange.swiss")),
            new("PCX", Message.Of("market_data.exchange.arca")),
            new("NMS", Message.Of("market_data.exchange.nasdaq"))
        ],
        Regions: [
            new("de", Message.Of("market_data.region.germany")),
            new("gb", Message.Of("market_data.region.britain")),
            new("nl", Message.Of("market_data.region.netherlands")),
            new("fr", Message.Of("market_data.region.france")),
            new("it", Message.Of("market_data.region.italy")),
            new("ch", Message.Of("market_data.region.switzerland")),
            new("us", Message.Of("market_data.region.usa"))
        ]);

    public FundScreenOptions Options() => ScreenOptions;

    public async Task<IReadOnlyList<FundScreenMatch>?> ScreenAsync(FundScreenQuery query, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.PostAsJsonAsync("/v1/funds/screen", query, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<ScreenResponse>(cancellationToken);

            if (body?.Funds is { } funds)
                return funds;

            logger.LogWarning("Fund screen returned no result list");
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Fund screen failed");
        }

        return null;
    }

    private sealed record ScreenResponse(IReadOnlyList<FundScreenMatch>? Funds);
}
