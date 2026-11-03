using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Strategies.Definitions.Application.Shared;

public sealed record VersionReportDraft(string Code, StrategyReportScheduleDto Schedule, IReadOnlyList<string>? SignalKinds, IReadOnlyList<Guid>? SignalIds);
