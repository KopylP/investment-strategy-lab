using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record AccountReport(
    string Code,
    string Name,
    string Description,
    string Mode,
    ReportKind Kind,
    IReadOnlyList<SubPortfolioReport> SubPortfolios,
    OwnReportSchedule? Own = null,
    IReadOnlyList<string>? SignalKinds = null)
{
    public static AccountReport? Choose(IReadOnlyList<AccountReport> reports, string? code) =>
        code is null
            ? reports.FirstOrDefault(report => report.Kind == ReportKind.Monthly)
            : reports.FirstOrDefault(report => report.Code == code);

    public bool ComesOn(string signalKind) => SignalKinds?.Contains(signalKind) == true;

    public Cadence CadenceOf(SubPortfolioReport sub) => Own?.Cadence ?? sub.Default;

    public IReadOnlyList<Guid> SubPortfoliosWithin(IReadOnlySet<Guid>? wanted)
    {
        var all = SubPortfolios.Select(sub => sub.SubPortfolioId);

        return wanted is null ? [.. all] : [.. all.Where(wanted.Contains)];
    }

    public decimal? ContributionOf(decimal? asked) => Kind == ReportKind.Monthly ? asked : null;
}
