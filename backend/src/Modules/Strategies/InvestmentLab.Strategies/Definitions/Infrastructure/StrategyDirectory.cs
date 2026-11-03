using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class StrategyDirectory(IStrategyRepository strategies, CatalogSynchronizer catalog) : IStrategyDirectory
{
    public async Task<IReadOnlyList<StrategySummaryDto>> GetStrategies(CancellationToken cancellationToken)
    {
        var types = (await catalog.ListAsync(cancellationToken)).ToDictionary(type => type.Code);

        return [.. (await strategies.ListAsync(cancellationToken)).Select(strategy => new StrategySummaryDto(
            strategy.Id.Value, strategy.Name, strategy.TypeCode, strategy.Latest.Number, strategy.Latest.Universe, strategy.Latest.CashInstrumentId,
            [.. (types.GetValueOrDefault(strategy.TypeCode)?.Reports ?? []).Select(report => new StrategyReportDto(
                report.Code, report.Name, report.Description, report.Mode, ToDto(report.Schedule ?? ReportSchedule.Off), report.SignalKinds))]))];
    }

    public async Task<StrategyReplaySetupDto?> GetReplaySetup(Guid strategyId, int? version, CancellationToken cancellationToken)
    {
        if (strategyId == Guid.Empty || await strategies.FindAsync(StrategyId.Of(strategyId), cancellationToken) is not { } strategy)
            return null;

        if (strategy.Version(version) is not { } chosen)
            return null;

        var types = await catalog.ListAsync(cancellationToken);
        var type = types.FirstOrDefault(candidate => candidate.Code == strategy.TypeCode);

        return new StrategyReplaySetupDto(
            StrategyId: strategy.Id.Value,
            OwnerId: strategy.OwnerId.Value,
            Version: chosen.Number,
            SupportsBacktest: type?.SupportsBacktest ?? false,
            Universe: chosen.Universe,
            CashInstrumentId: chosen.CashInstrumentId,
            BenchmarkInstrumentId: chosen.BenchmarkInstrumentId,
            Reports: ReportsOf(type),
            Comparisons: type?.BacktestComparisons ?? []);
    }

    private static List<StrategyReportDto> ReportsOf(StrategyTypeDescriptor? type) =>
        [.. (type?.Reports ?? []).Select(report => new StrategyReportDto(
            report.Code, report.Name, report.Description, report.Mode, ToDto(report.Schedule ?? ReportSchedule.Off), report.SignalKinds))];

    public async Task<IReadOnlyDictionary<string, string>> GetMessages(CancellationToken cancellationToken)
    {
        var types = await catalog.ListFromEngineAsync(cancellationToken);

        return types.SelectMany(type => type.Messages).DistinctBy(message => message.Key).ToDictionary();
    }

    private static StrategyReportScheduleDto ToDto(ReportSchedule schedule) =>
        new(schedule.Frequency, schedule.Day, schedule.Weekday, schedule.Time, schedule.Minute);
}
