using InvestmentLab.Strategies.Catalog.Domain;
using System.Text.Json;
using InvestmentLab.Strategies.Definitions.Domain;
using Json.Schema;

namespace InvestmentLab.Strategies.Definitions.Application.Shared;

internal static class StrategyMapping
{
    extension(Strategy strategy)
    {
        public StrategyDto ToDto(StrategyTypeDescriptor? type, StrategyVersion? version = null)
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
                    shown.CreatedAt,
                    shown.ExtraSeries),
                EffectiveReportMapping.ReportsOf(type, shown),
                strategy.DeletedSignalIds);
        }
    }
}
