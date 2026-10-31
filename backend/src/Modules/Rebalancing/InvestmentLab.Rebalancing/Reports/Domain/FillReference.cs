namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record FillReference(ReportId ReportId, ReportActionId ActionId, int FillNumber)
{
    private const string Prefix = "rebalancing";

    public override string ToString() => $"{Prefix}:{ReportId}:{ActionId}:{FillNumber}";

    public static FillReference? TryParse(string? text) =>
        text?.Split(':') is [Prefix, var report, var action, var number]
        && Guid.TryParse(report, out var reportId) && reportId != Guid.Empty
        && Guid.TryParse(action, out var actionId) && actionId != Guid.Empty
        && int.TryParse(number, out var fillNumber)
            ? new FillReference(ReportId.Of(reportId), ReportActionId.Of(actionId), fillNumber)
            : null;
}
