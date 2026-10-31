using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Performance.Valuation.Application.Shared;

public sealed class AccountHistoryLoader(IPerformanceRepository store)
{
    public async Task<AccountHistory> LoadAsync(AccountSetupDto account, CancellationToken cancellationToken)
    {
        var snapshots = await store.SnapshotsAsync(account.Id, SnapshotScenario.Actual, DateOnly.MinValue, cancellationToken);
        var planned = await store.PlannedReportsAsync(account.Id, cancellationToken);
        var plan = planned.Any(report => report.CountsForPlan) ? await PlanAsync(account.Id, cancellationToken) : null;

        var reports = planned.Where(report => !report.IsSuperseded)
            .OrderBy(report => report.MarketAsOf)
            .Select(report => new ReportMarkerDto(report.ReportId, report.MarketAsOf, report.Kind, report.Period, account.Id))
            .ToList();

        return new AccountHistory(
            Series: [.. snapshots.Where(snapshot => snapshot.SubPortfolioId is null).OrderBy(snapshot => snapshot.Date).Select(ToSnapshot)],
            SubPortfolios: [.. snapshots.Where(snapshot => snapshot.SubPortfolioId is not null).Select(ToSnapshot)],
            Plan: plan,
            Reports: reports);
    }

    private async Task<Dictionary<DateOnly, PlanPoint>> PlanAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var snapshots = await store.SnapshotsAsync(accountId, SnapshotScenario.FullPlan, DateOnly.MinValue, cancellationToken);

        return snapshots
            .Where(snapshot => snapshot.SubPortfolioId is null)
            .ToDictionary(snapshot => snapshot.Date, snapshot => new PlanPoint(snapshot.Value, snapshot.Flow, snapshot.NetContributions));
    }

    private static Snapshot ToSnapshot(ValuationSnapshot snapshot) => new(
        snapshot.Date,
        snapshot.SubPortfolioId,
        snapshot.Value,
        snapshot.Cash,
        snapshot.Flow,
        snapshot.NetContributions,
        snapshot.TwrIndex,
        snapshot.CostBasis);
}
