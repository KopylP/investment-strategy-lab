namespace InvestmentLab.Strategies.Contracts;

public sealed record StrategySummaryDto(
    Guid Id,
    string Name,
    string TypeCode,
    int LatestVersion,
    IReadOnlyList<Guid> Universe,
    Guid? CashInstrumentId,
    IReadOnlyList<StrategyReportDto> Reports);
