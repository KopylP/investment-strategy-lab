namespace InvestmentLab.Strategies.Catalog.Application.Shared;

public sealed record ReportDefinitionDto(
    string Code,
    string Name,
    string Description,
    string Mode,
    ReportScheduleDto? Schedule,
    IReadOnlyList<string>? SignalKinds);
