using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application.GetAuthoredVersion;

public sealed class GetAuthoredVersionHandler(IStrategyRepository strategies)
{
    public async Task<AuthoredVersionDto?> Handle(Guid strategyId, int version, CancellationToken cancellationToken)
    {
        if (strategyId == Guid.Empty)
            return null;

        var strategy = await strategies.FindAsync(StrategyId.Of(strategyId), cancellationToken);
        var shown = strategy?.Version(version);
        if (strategy is null || shown is null)
            return null;

        var reports = shown.Reports.Select(ReportOf).ToList();
        var setup = new AuthoredSetupDto(
            TypeCode: strategy.TypeCode,
            Parameters: shown.Parameters,
            Universe: shown.Universe,
            BenchmarkInstrumentId: shown.BenchmarkInstrumentId,
            CashInstrumentId: shown.CashInstrumentId,
            VolatilityIndexInstrumentId: shown.VolatilityIndexInstrumentId,
            ExtraSeries: shown.ExtraSeries,
            Reports: reports);

        return new AuthoredVersionDto(strategy.Name, strategy.TypeCode, setup);
    }

    private static AuthoredReportDto ReportOf(VersionReport report)
    {
        var timing = report.Schedule;
        var schedule = new StrategyReportScheduleDto(
            Frequency: timing.Frequency,
            Day: timing.Day,
            Weekday: timing.Weekday,
            Time: timing.Time,
            Minute: timing.Minute);

        return new AuthoredReportDto(report.Code, schedule, report.SignalKinds, report.SignalIds);
    }
}
