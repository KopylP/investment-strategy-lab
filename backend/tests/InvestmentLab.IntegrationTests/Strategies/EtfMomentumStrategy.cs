using System.Net.Http.Json;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Strategies.Definitions.Application.Shared;

namespace InvestmentLab.IntegrationTests.Strategies;

internal static class EtfMomentumStrategy
{
    extension(HttpClient client)
    {
        public async Task<StrategyDto> CreateEtfMomentumOnAsync(StrategyDto template, CancellationToken cancellationToken)
        {
            var instruments = await client.GetFromJsonAsync<List<InstrumentDto>>("/api/market-data/instruments", cancellationToken);
            var volatilityIndex = instruments!.Single(instrument => instrument.IsVolatilityIndex).Id;

            var created = await client.PostAsJsonAsync("/api/strategies", new
            {
                name = $"{template.Name} as ETF momentum",
                typeCode = "etf-momentum",
                setup = new
                {
                    universe = template.Version.Universe,
                    benchmarkInstrumentId = template.Version.BenchmarkInstrumentId,
                    volatilityIndexInstrumentId = volatilityIndex,
                    cashInstrumentId = template.Version.CashInstrumentId,
                },
            }, cancellationToken);
            created.EnsureSuccessStatusCode();

            return (await created.Content.ReadFromJsonAsync<StrategyDto>(cancellationToken))!;
        }
    }
}
