namespace InvestmentLab.Signals.Contracts;

public sealed record SignalTriggeredV1(
    Guid TriggerId,
    Guid SignalId,
    Guid OwnerId,
    Guid? AccountId,
    string Name,
    string Metric,
    DateOnly DataDate,
    DateTimeOffset TriggeredAt,
    IReadOnlyList<TriggeredSubjectDto> Subjects);
