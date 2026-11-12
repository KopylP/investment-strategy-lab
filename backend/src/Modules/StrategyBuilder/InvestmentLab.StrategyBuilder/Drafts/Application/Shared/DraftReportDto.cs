namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed record DraftReportDto(string Code, ReportTimingDto Schedule, IReadOnlyList<string> SignalKinds, IReadOnlyList<Guid> SignalIds);
