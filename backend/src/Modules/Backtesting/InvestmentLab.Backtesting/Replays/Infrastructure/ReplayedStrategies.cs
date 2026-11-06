using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class ReplayedStrategies(IStrategyDirectory strategies, IInstrumentCatalog instruments) : IReplayedStrategies
{
    public async Task<ReplaySetup?> FindAsync(Guid strategyId, int? version, CancellationToken cancellationToken)
    {
        if (await strategies.GetReplaySetup(strategyId, version, cancellationToken) is not { } setup)
            return null;

        var catalog = await instruments.GetInstruments(cancellationToken);
        var currency = ReplayCurrency.Of(TradingCurrenciesOf(setup.Universe, catalog));
        var rebalance = CadenceOf(setup, StrategyReportDto.Rebalance);
        var signals = SignalTriggersOf(setup, catalog.FirstOrDefault(instrument => instrument.IsVolatilityIndex)?.Id);
        var benchmark = BenchmarkOf(setup.BenchmarkInstrumentId, catalog);

        return new ReplaySetup(
            StrategyId: setup.StrategyId,
            OwnerId: UserId.Of(setup.OwnerId),
            Version: setup.Version,
            CanBeReplayed: setup.SupportsBacktest,
            Universe: setup.Universe,
            CashInstrumentId: setup.CashInstrumentId,
            Benchmark: benchmark,
            RebalanceCadence: rebalance ?? ReplayCadence.Monthly,
            RiskReviewCadence: CadenceOf(setup, StrategyReportDto.RiskReview),
            Signals: signals,
            Currency: currency,
            Comparisons: setup.Comparisons,
            LogicSignalIds: setup.LogicSignalIds ?? [],
            HistoryBars: setup.HistoryBars,
            ExtraSeries: setup.ExtraSeries,
            RebalanceSchedule: ScheduleOf(setup, StrategyReportDto.Rebalance),
            RiskReviewSchedule: ScheduleOf(setup, StrategyReportDto.RiskReview),
            UnsupportedSchedule: UnsupportedScheduleOf(setup));
    }

    private static ReplayBenchmark BenchmarkOf(Guid instrumentId, IReadOnlyList<InstrumentDto> catalog)
    {
        var instrument = catalog.FirstOrDefault(candidate => candidate.Id == instrumentId)
            ?? throw new InvalidOperationException($"Benchmark {instrumentId} of the strategy is not in the catalog.");

        return new ReplayBenchmark(instrumentId, instrument.Label);
    }

    private static List<string> TradingCurrenciesOf(IReadOnlyList<Guid> universe, IReadOnlyList<InstrumentDto> catalog)
    {
        var byId = catalog.ToDictionary(instrument => instrument.Id);

        return [.. universe.Select(id => byId.TryGetValue(id, out var instrument)
            ? instrument.TradingCurrency
            : throw new InvalidOperationException($"Instrument {id} of the strategy is not in the catalog."))];
    }

    private static ReplaySignalTriggers SignalTriggersOf(StrategyReplaySetupDto setup, Guid? volatilityIndexId)
    {
        var rebalance = SignalKindsOf(setup, StrategyReportDto.Rebalance);
        var riskReview = SignalKindsOf(setup, StrategyReportDto.RiskReview);

        return new ReplaySignalTriggers(rebalance, riskReview, volatilityIndexId,
            SignalIdsOf(setup, StrategyReportDto.Rebalance), SignalIdsOf(setup, StrategyReportDto.RiskReview));
    }

    private static IReadOnlyList<Guid> SignalIdsOf(StrategyReplaySetupDto setup, string mode) =>
        [.. setup.Reports.Where(report => report.Mode == mode).SelectMany(report => report.SignalIds ?? []).Distinct()];

    private static IReadOnlyList<string> SignalKindsOf(StrategyReplaySetupDto setup, string mode) =>
        [.. setup.Reports.Where(report => report.Mode == mode).SelectMany(report => report.SignalKinds ?? []).Distinct()];

    private static ReplayCadence? CadenceOf(StrategyReplaySetupDto setup, string mode)
    {
        var report = setup.Reports.FirstOrDefault(candidate => candidate.Mode == mode);

        return report?.Schedule.Frequency.ToUpperInvariant() switch
        {
            null or "OFF" => null,
            "HOURLY" or "DAILY" => ReplayCadence.Daily,
            "WEEKLY" => ReplayCadence.Weekly,
            "MONTHLY" => ReplayCadence.Monthly,
            "QUARTERLY" => ReplayCadence.Quarterly,
            var unknown => throw new InvalidOperationException($"Unknown report frequency '{unknown}'."),
        };
    }

    private static ReplaySchedule? ScheduleOf(StrategyReplaySetupDto setup, string mode)
    {
        var report = setup.Reports.FirstOrDefault(candidate => candidate.Mode == mode);
        var cadence = CadenceOf(setup, mode);
        return report is null || cadence is null ? null : new ReplaySchedule(cadence.Value, report.Schedule.Day,
            Enum.TryParse<DayOfWeek>(report.Schedule.Weekday, true, out var weekday) ? weekday : DayOfWeek.Sunday);
    }

    private static string? UnsupportedScheduleOf(StrategyReplaySetupDto setup)
    {
        var unsupported = setup.Reports.FirstOrDefault(report =>
            !string.Equals(report.Schedule.Frequency, "off", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(report.Schedule.Frequency, "hourly", StringComparison.OrdinalIgnoreCase) || report.Schedule.Time != "20:00"));
        return unsupported is null ? null : $"{unsupported.Code}: {unsupported.Schedule.Frequency} at {unsupported.Schedule.Time}";
    }
}
