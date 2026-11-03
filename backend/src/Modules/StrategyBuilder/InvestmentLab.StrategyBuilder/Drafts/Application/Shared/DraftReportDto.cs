using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed record DraftReportDto(string Code, ReportTiming Schedule, IReadOnlyList<string> SignalKinds, IReadOnlyList<Guid> SignalIds);
