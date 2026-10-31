using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Performance.Valuation.Application.GetDashboard;

public sealed class GetDashboardHandler(AccountHistoryLoader histories, IAccountSetupQueries accounts)
{
    public async Task<Result<DashboardDto>> Handle(Guid accountId, string? period, CancellationToken cancellationToken)
    {
        if (await accounts.GetAccountSetup(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var history = await histories.LoadAsync(account, cancellationToken);
        var key = DashboardPeriod.KeyOf(period);

        if (history.Series.Count == 0)
            return DashboardSummary.Empty(accountId, account.BaseCurrency, key, history.Reports);

        var summary = DashboardSummary.Of(history.Series, history.Plan, key);

        return summary.Dashboard with
        {
            AccountId = accountId,
            Currency = account.BaseCurrency,
            SubPortfolios = SubPortfolioRows(account, history, summary.Before),
            Reports = [.. history.Reports.Where(report => report.Date >= summary.Start)],
        };
    }

    private static List<SubPortfolioPerformanceDto> SubPortfolioRows(AccountSetupDto account, AccountHistory history, Snapshot? before)
    {
        var last = history.Series[^1];
        var latest = history.SubPortfolios.Where(snapshot => snapshot.Date == last.Date).ToDictionary(snapshot => snapshot.SubPortfolioId!.Value);
        var indexBefore = before is null
            ? []
            : history.SubPortfolios.Where(snapshot => snapshot.Date == before.Date).ToDictionary(snapshot => snapshot.SubPortfolioId!.Value, snapshot => snapshot.TwrIndex);

        var shown = account.SubPortfolios.Where(sub => sub.Status == "Active" || latest.ContainsKey(sub.Id));

        return [.. shown.Select(sub => RowOf(sub, latest.GetValueOrDefault(sub.Id), indexBefore.GetValueOrDefault(sub.Id, 1m), last.Value))];
    }

    private static SubPortfolioPerformanceDto RowOf(SubPortfolioSetupDto sub, Snapshot? snapshot, decimal startIndex, decimal accountValue)
    {
        var value = snapshot?.Value ?? 0m;

        return new SubPortfolioPerformanceDto(
            Id: sub.Id,
            Name: sub.Name,
            Value: value,
            TargetShare: sub.TargetShare,
            ActualShare: accountValue != 0m ? value / accountValue : 0m,
            Twr: snapshot is null || startIndex == 0m ? null : snapshot.TwrIndex / startIndex - 1m,
            NetContributions: snapshot?.NetContributions ?? 0m);
    }
}
