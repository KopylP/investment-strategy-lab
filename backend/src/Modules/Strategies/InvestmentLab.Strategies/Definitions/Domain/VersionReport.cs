using InvestmentLab.Strategies.Catalog.Domain;

namespace InvestmentLab.Strategies.Definitions.Domain;

public sealed record VersionReport(string Code, ReportSchedule Schedule, IReadOnlyList<string> SignalKinds, IReadOnlyList<Guid> SignalIds);
