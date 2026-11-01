using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class AccountReports(IPlanningAccounts accounts, IStrategyDirectory strategies) : IAccountReports
{
    public async Task<IReadOnlyList<AccountReport>> OfAsync(Guid accountId, PlanSettings? settings, CancellationToken cancellationToken)
    {
        var account = await accounts.FindAsync(accountId, cancellationToken);
        if (account is null)
            return [];

        var known = await strategies.GetStrategies(cancellationToken);
        var directory = known.ToDictionary(strategy => strategy.Id);

        var declared = PlannedSubPortfolios.Of(account).SelectMany(sub => DeclaredBy(sub, directory));

        return AccountReport.AllOf(declared, settings);
    }

    private static IEnumerable<DeclaredReport> DeclaredBy(IPlanningSubPortfolio sub, Dictionary<Guid, StrategySummaryDto> directory) =>
        directory.TryGetValue(sub.StrategyId!.Value, out var strategy)
            ? strategy.Reports.Select(report => DeclaredReportOf(sub, strategy, report))
            : [];

    private static DeclaredReport DeclaredReportOf(IPlanningSubPortfolio sub, StrategySummaryDto strategy, StrategyReportDto report) => new(
        Code: report.Code,
        Name: report.Name,
        Description: report.Description,
        Mode: report.Mode,
        Kind: report.Mode == StrategyReportDto.RiskReview ? ReportKind.RiskReview : ReportKind.Monthly,
        SubPortfolio: new SubPortfolioReport(sub.Id, sub.Name, strategy.Name, CadenceOf(report.Schedule)),
        SignalKinds: report.SignalKinds ?? []);

    private static Cadence CadenceOf(StrategyReportScheduleDto schedule) =>
        Cadence.Of(schedule.Frequency, schedule.Day, schedule.Weekday, schedule.Time, schedule.Minute);
}
