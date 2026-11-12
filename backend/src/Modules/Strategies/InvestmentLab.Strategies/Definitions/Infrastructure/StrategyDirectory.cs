using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class StrategyDirectory(IStrategyRepository strategies, CatalogSynchronizer catalog, IParameterChecks checks) : IStrategyDirectory
{
    public async Task<IReadOnlyList<StrategySummaryDto>> GetStrategies(CancellationToken cancellationToken)
    {
        var types = (await catalog.ListAsync(cancellationToken)).ToDictionary(type => type.Code);

        return [.. (await strategies.ListAsync(cancellationToken)).Select(strategy => new StrategySummaryDto(
            strategy.Id.Value, strategy.Name, strategy.TypeCode, strategy.Latest.Number, strategy.Latest.Universe, strategy.Latest.CashInstrumentId,
            EffectiveReportMapping.ReportsOf(types.GetValueOrDefault(strategy.TypeCode), strategy.Latest)))];
    }

    public async Task<StrategyReplaySetupDto?> GetReplaySetup(Guid strategyId, int? version, CancellationToken cancellationToken)
    {
        if (strategyId == Guid.Empty || await strategies.FindAsync(StrategyId.Of(strategyId), cancellationToken) is not { } strategy)
            return null;

        if (strategy.Version(version) is not { } chosen)
            return null;

        var type = await catalog.FindAsync(strategy.TypeCode, cancellationToken);
        var check = await checks.CheckAsync(strategy.TypeCode, chosen.Parameters, cancellationToken);
        if (check.IsFailure)
            throw new InvalidOperationException(check.Error.Message);

        return new StrategyReplaySetupDto(
            StrategyId: strategy.Id.Value,
            OwnerId: strategy.OwnerId.Value,
            Version: chosen.Number,
            SupportsBacktest: type?.SupportsBacktest ?? false,
            Universe: chosen.Universe,
            CashInstrumentId: chosen.CashInstrumentId,
            BenchmarkInstrumentId: chosen.BenchmarkInstrumentId,
            Reports: EffectiveReportMapping.ReportsOf(type, chosen),
            Comparisons: type?.BacktestComparisons ?? [],
            LogicSignalIds: chosen.SignalIds,
            HistoryBars: check.Value.HistoryBars,
            ExtraSeries: chosen.ExtraSeries);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetMessages(string languageCode, CancellationToken cancellationToken)
    {
        var types = await catalog.ListFromEngineAsync(cancellationToken);
        var messages = types.SelectMany(type => type.MessagesIn(languageCode));

        return messages.DistinctBy(message => message.Key).ToDictionary();
    }
}
