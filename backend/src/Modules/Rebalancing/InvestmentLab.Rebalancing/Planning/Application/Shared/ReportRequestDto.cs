namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

public sealed record ReportRequestDto(
    Guid Id,
    Guid AccountId,
    string Code,
    string Title,
    string Kind,
    string Trigger,
    string Status,
    DateOnly AsOf,
    DateTimeOffset RequestedAt,
    DateTimeOffset Deadline,
    DateTimeOffset? CompletedAt,
    Guid? ReportId,
    string? Error,
    int Strategies,
    int StrategiesAnswered);
