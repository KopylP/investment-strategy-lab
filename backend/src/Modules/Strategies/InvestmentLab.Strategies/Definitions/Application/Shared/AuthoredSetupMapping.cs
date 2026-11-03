using System.Text.Json;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Strategies.Definitions.Application.Shared;

public static class AuthoredSetupMapping
{
    public static StrategySetupDraft ToDraft(AuthoredSetupDto setup) =>
        new(
            Universe: setup.Universe,
            BenchmarkInstrumentId: setup.BenchmarkInstrumentId,
            VolatilityIndexInstrumentId: setup.VolatilityIndexInstrumentId,
            CashInstrumentId: setup.CashInstrumentId,
            Parameters: JsonDocument.Parse(setup.Parameters).RootElement,
            Reports: [.. setup.Reports.Select(ToDraft)],
            ExtraSeries: setup.ExtraSeries);

    private static VersionReportDraft ToDraft(AuthoredReportDto report) =>
        new(report.Code, report.Schedule, report.SignalKinds, report.SignalIds);
}
