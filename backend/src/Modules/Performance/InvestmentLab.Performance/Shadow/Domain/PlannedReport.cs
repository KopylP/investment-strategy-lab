using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Shadow.Domain;

public sealed class PlannedReport
{
    public Guid ReportId { get; private set; }
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public string Kind { get; private set; }
    public string Period { get; private set; }
    public DateOnly MarketAsOf { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }
    public List<PlannedAction> Actions { get; private set; }
    public bool IsSuperseded { get; private set; }

    private PlannedReport(Guid reportId, Guid accountId, UserId ownerId, string kind, string period, DateOnly marketAsOf, DateTimeOffset generatedAt, IReadOnlyList<PlannedAction> actions) =>
        (ReportId, AccountId, OwnerId, Kind, Period, MarketAsOf, GeneratedAt, Actions) = (reportId, accountId, ownerId, kind, period, marketAsOf, generatedAt, [.. actions]);

    private PlannedReport() => (Kind, Period, Actions) = (null!, null!, null!);

    public static PlannedReport Of(Guid reportId, Guid accountId, UserId ownerId, string kind, string period, DateOnly marketAsOf, DateTimeOffset generatedAt, IReadOnlyList<PlannedAction> actions) =>
        new(reportId, accountId, ownerId, kind, period, marketAsOf, generatedAt, actions);

    public void Supersede() => IsSuperseded = true;

    public bool CountsForPlan => !IsSuperseded && Kind is "Monthly" or "Recalculation";
}
