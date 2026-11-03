using InvestmentLab.Strategies.Catalog.Domain;

namespace InvestmentLab.Strategies.Definitions.Domain;

public sealed record EffectiveReport(
    string Code,
    string Name,
    string Description,
    string Mode,
    ReportSchedule Schedule,
    IReadOnlyList<string> SignalKinds,
    IReadOnlyList<Guid> SignalIds);
