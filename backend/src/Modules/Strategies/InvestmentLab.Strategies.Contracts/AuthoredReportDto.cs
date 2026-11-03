namespace InvestmentLab.Strategies.Contracts;

public sealed record AuthoredReportDto(string Code, StrategyReportScheduleDto Schedule, IReadOnlyList<string> SignalKinds, IReadOnlyList<Guid> SignalIds);
