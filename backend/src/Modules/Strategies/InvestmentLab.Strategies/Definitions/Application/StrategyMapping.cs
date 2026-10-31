using System.Text.Json;
using Json.Schema;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application;

internal static class StrategyMapping
{
    extension(Strategy strategy)
    {
        public StrategyDto ToDto(StrategyVersion? version = null)
        {
            var shown = version ?? strategy.Latest;

            return new StrategyDto(
                strategy.Id.Value,
                strategy.Name,
                strategy.TypeCode,
                strategy.Status.ToString(),
                strategy.CreatedAt,
                strategy.Latest.Number,
                new StrategyVersionDto(
                    shown.Number,
                    JsonDocument.Parse(shown.Parameters).RootElement,
                    shown.Universe,
                    shown.CashInstrumentId,
                    shown.BenchmarkInstrumentId,
                    shown.VolatilityIndexInstrumentId,
                    shown.CreatedAt));
        }
    }
}
