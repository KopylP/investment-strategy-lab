using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.IntegrationTests.MarketData;

public sealed class FakeFundScreener : IFundScreener
{
    public bool Unavailable { get; set; }

    public FundScreenOptions Options() => new(
        [new("place", "@market_data.exchange.amsterdam")],
        [new("region", "@market_data.region.netherlands")]);

    public Task<IReadOnlyList<FundScreenMatch>?> ScreenAsync(FundScreenQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<FundScreenMatch>? funds = Unavailable
            ? null
            : [
                new("SXR8.DE", "Known fund", "EUR", null, null, 3_000_000_000m, 0.002m),
                new("FUND08.AS", "Discovered fund", "EUR", null, null, 2_000_000_000m, 0.001m),
                new("NO_CURRENCY", "Unknown currency", null, null, null, null, null)
            ];

        return Task.FromResult(funds);
    }
}
