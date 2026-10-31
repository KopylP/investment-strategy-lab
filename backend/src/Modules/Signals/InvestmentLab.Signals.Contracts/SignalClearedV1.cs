namespace InvestmentLab.Signals.Contracts;

public sealed record SignalClearedV1(
    Guid SignalId,
    Guid OwnerId,
    string Name,
    string Reason,
    IReadOnlyList<string> Subjects,
    int StillActive,
    DateTimeOffset ClearedAt);
