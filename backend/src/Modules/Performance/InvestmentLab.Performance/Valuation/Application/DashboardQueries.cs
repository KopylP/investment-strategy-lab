using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Performance.Valuation.Application;

public sealed record SeriesPointDto(DateOnly Date, decimal Value, decimal NetContributions, decimal TwrIndex, decimal? PlanValue, decimal Drawdown);

public sealed record SubPortfolioPerformanceDto(Guid Id, string Name, decimal Value, decimal TargetShare, decimal ActualShare, decimal? Twr, decimal NetContributions);

public sealed record ReportMarkerDto(Guid ReportId, DateOnly Date, string Kind, string Period);

public sealed record DashboardDto(
    Guid AccountId,
    string Currency,
    DateOnly? AsOf,
    string Period,
    decimal Value,
    decimal NetContributions,
    decimal Gain,
    decimal? Twr,
    decimal? TwrAnnualized,
    decimal? Xirr,
    decimal MaxDrawdown,
    decimal CurrentDrawdown,
    decimal? PlanValue,
    decimal? TaxDueThisYearPln,
    IReadOnlyList<SeriesPointDto> Series,
    IReadOnlyList<SubPortfolioPerformanceDto> SubPortfolios,
    IReadOnlyList<ReportMarkerDto> Reports);

/// <summary>The dashboard (§6.8 GetDashboard): value over time, returns, drawdown, plan vs actual, allocation.</summary>
public sealed class DashboardQueries(SnapshotBuilder builder, IPerformanceRepository store, IAccountSetupQueries accounts, TimeProvider clock)
{
    public async Task<Result<DashboardDto>> Get(Guid accountId, string? period, CancellationToken cancellationToken)
    {
        if (await accounts.GetAccountSetup(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");
        await builder.EnsureFreshAsync(accountId, cancellationToken);

        var all = await store.SnapshotsAsync(accountId, SnapshotScenario.Actual, DateOnly.MinValue, cancellationToken);
        var accountSeries = all.Where(s => s.SubPortfolioId is null).OrderBy(s => s.Date).ToList();
        var planned = await store.PlannedReportsAsync(accountId, cancellationToken);
        // Without a plan the shadow is the journal itself: nothing to compare.
        var plan = planned.Any(report => report.CountsForPlan)
            ? (await store.SnapshotsAsync(accountId, SnapshotScenario.FullPlan, DateOnly.MinValue, cancellationToken)).ToDictionary(s => s.Date, s => s.Value)
            : [];
        var reports = planned.Where(report => !report.IsSuperseded)
            .OrderBy(report => report.MarketAsOf).Select(report => new ReportMarkerDto(report.ReportId, report.MarketAsOf, report.Kind, report.Period)).ToList();
        var taxes = await store.TaxTotalsAsync(accountId, cancellationToken);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var taxThisYear = taxes.FirstOrDefault(total => total.Year == today.Year)?.TaxDuePln;
        var key = string.IsNullOrWhiteSpace(period) ? "ALL" : period.ToUpperInvariant();

        if (accountSeries.Count == 0)
            return new DashboardDto(accountId, account.BaseCurrency, null, key, 0m, 0m, 0m, null, null, null, 0m, 0m, null, taxThisYear, [], [], reports);

        var last = accountSeries[^1];
        var start = PeriodStart(key, last.Date);
        var window = accountSeries.Where(s => s.Date >= start).ToList();
        var before = accountSeries.LastOrDefault(s => s.Date < start);
        var baseIndex = before?.TwrIndex ?? 1m;
        var periodTwr = baseIndex > 0m ? last.TwrIndex / baseIndex - 1m : (decimal?)null;
        var years = (last.Date.DayNumber - (before?.Date ?? accountSeries[0].Date).DayNumber) / 365.25m;
        decimal? annualized = periodTwr is { } twr && years >= 1m ? (decimal)Math.Pow((double)(1m + twr), (double)(1m / years)) - 1m : null;

        var drawdown = DrawdownCalculator.Of([.. window.Select(s => (s.Date, s.TwrIndex))]);
        var peak = 0m;
        var series = window.Select(s =>
        {
            peak = Math.Max(peak, s.TwrIndex);
            return new SeriesPointDto(s.Date, s.Value, s.NetContributions, s.TwrIndex / baseIndex, plan.TryGetValue(s.Date, out var planned) ? planned : null,
                peak > 0m ? decimal.Round(s.TwrIndex / peak - 1m, 6) : 0m);
        }).ToList();

        var flows = window.Where(s => s.Flow != 0m).Select(s => (s.Date, -s.Flow)).ToList();
        if (before is not null && before.Value != 0m)
            flows.Insert(0, (before.Date, -before.Value));
        flows.Add((last.Date, last.Value));
        var xirr = XirrCalculator.Of(flows);

        var latestBySub = all.Where(s => s.SubPortfolioId is not null && s.Date == last.Date).ToDictionary(s => s.SubPortfolioId!.Value);
        var subBefore = before is null ? [] : all.Where(s => s.SubPortfolioId is not null && s.Date == before.Date).ToDictionary(s => s.SubPortfolioId!.Value, s => s.TwrIndex);
        var subs = account.SubPortfolios.Where(sub => sub.Status == "Active" || latestBySub.ContainsKey(sub.Id)).Select(sub =>
        {
            var snapshot = latestBySub.GetValueOrDefault(sub.Id);
            var startIndex = subBefore.GetValueOrDefault(sub.Id, 1m);
            return new SubPortfolioPerformanceDto(sub.Id, sub.Name, snapshot?.Value ?? 0m, sub.TargetShare,
                last.Value != 0m ? (snapshot?.Value ?? 0m) / last.Value : 0m,
                snapshot is null || startIndex == 0m ? null : snapshot.TwrIndex / startIndex - 1m,
                snapshot?.NetContributions ?? 0m);
        }).ToList();

        return new DashboardDto(
            accountId, account.BaseCurrency, last.Date, key, last.Value, last.NetContributions, last.Value - last.NetContributions,
            periodTwr, annualized, xirr, drawdown.Max, drawdown.Current, plan.TryGetValue(last.Date, out var planNow) ? planNow : null,
            taxThisYear, series, subs, [.. reports.Where(report => report.Date >= start)]);
    }

    private static DateOnly PeriodStart(string period, DateOnly last) => period switch
    {
        "1M" => last.AddMonths(-1),
        "3M" => last.AddMonths(-3),
        "6M" => last.AddMonths(-6),
        "1Y" => last.AddYears(-1),
        "YTD" => new DateOnly(last.Year, 1, 1),
        _ => DateOnly.MinValue,
    };
}
