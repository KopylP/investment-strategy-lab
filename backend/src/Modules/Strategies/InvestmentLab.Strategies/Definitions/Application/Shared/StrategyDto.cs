using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Strategies.Definitions.Application.Shared;

public sealed record StrategyDto(
    Guid Id,
    string Name,
    string TypeCode,
    string Status,
    DateTimeOffset CreatedAt,
    int LatestVersion,
    StrategyVersionDto Version,
    IReadOnlyList<StrategyReportDto> Reports,
    IReadOnlyList<Guid> DeletedSignalIds);
