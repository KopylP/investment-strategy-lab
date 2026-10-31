using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Performance.Valuation.Application;

public sealed class DashboardQueries(SnapshotBuilder builder, IPerformanceRepository store, IAccountSetupQueries accounts, PriceBook prices)
{
    public async Task<Result<DashboardDto>> Get(Guid accountId, string? period, CancellationToken cancellationToken)
    {
        if (await accounts.GetAccountSetup(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var history = await HistoryAsync(account, cancellationToken);
        var key = PeriodKey(period);

        if (history.Series.Count == 0)
            return Empty(accountId, account.BaseCurrency, key, history.Reports);

        var (summary, start, before) = Summarize(history.Series, history.Plan, key);
        var last = history.Series[^1];
        var latestBySub = history.Subs.Where(s => s.Date == last.Date).ToDictionary(s => s.SubPortfolioId!.Value);
        var subBefore = before is null ? [] : history.Subs.Where(s => s.Date == before.Date).ToDictionary(s => s.SubPortfolioId!.Value, s => s.TwrIndex);
        var subs = account.SubPortfolios.Where(sub => sub.Status == "Active" || latestBySub.ContainsKey(sub.Id)).Select(sub =>
        {
            var snapshot = latestBySub.GetValueOrDefault(sub.Id);
            var startIndex = subBefore.GetValueOrDefault(sub.Id, 1m);

            return new SubPortfolioPerformanceDto(sub.Id, sub.Name, snapshot?.Value ?? 0m, sub.TargetShare,
                last.Value != 0m ? (snapshot?.Value ?? 0m) / last.Value : 0m,
                snapshot is null || startIndex == 0m ? null : snapshot.TwrIndex / startIndex - 1m,
                snapshot?.NetContributions ?? 0m);
        }).ToList();

        return summary with
        {
            AccountId = accountId,
            Currency = account.BaseCurrency,
            SubPortfolios = subs,
            Reports = [.. history.Reports.Where(report => report.Date >= start)],
        };
    }

    public async Task<Result<DashboardDto>> GetAll(string? currency, string? period, CancellationToken cancellationToken)
    {
        var setups = (await accounts.GetAccountSetups(cancellationToken)).OrderBy(account => account.Name).ToList();
        var key = PeriodKey(period);
        var target = string.IsNullOrWhiteSpace(currency) ? setups.FirstOrDefault()?.BaseCurrency ?? "EUR" : currency.ToUpperInvariant();
        if (target.Length != 3)
            return Error.Validation("dashboard.currency_invalid", $"{currency} is not a currency code.");

        var histories = new List<(AccountSetupDto Account, AccountHistory History)>();

        foreach (var setup in setups)
            histories.Add((setup, await HistoryAsync(setup, cancellationToken)));

        var reports = histories.SelectMany(entry => entry.History.Reports).OrderBy(report => report.Date).ToList();
        var withHistory = histories.Where(entry => entry.History.Series.Count > 0).ToList();
        if (withHistory.Count == 0)
            return Empty(null, target, key, reports) with { Accounts = [.. setups.Select(setup => new AccountPerformanceDto(setup.Id, setup.Name, setup.Broker, setup.BaseCurrency, 0m, 0m, 0m, null))] };

        var days = withHistory.SelectMany(entry => entry.History.Series.Select(s => s.Date)).Distinct().Order().ToList();
        var fx = await prices.LoadFxAsync([.. withHistory.Select(entry => entry.Account.BaseCurrency)], target, days[0], days[^1], cancellationToken);
        var (series, plan) = Combine(withHistory, days, fx);

        var (summary, start, before) = Summarize(series, plan, key);
        var last = series[^1];
        var rows = histories.Select(entry =>
        {
            var own = entry.History.Series;
            var latest = own.LastOrDefault(s => s.Date <= last.Date);
            var rate = latest is null ? 0m : fx(entry.Account.BaseCurrency, last.Date) ?? 0m;
            var startIndex = own.LastOrDefault(s => s.Date < start)?.TwrIndex ?? 1m;
            var value = decimal.Round((latest?.Value ?? 0m) * rate, 2);

            return new AccountPerformanceDto(entry.Account.Id, entry.Account.Name, entry.Account.Broker, entry.Account.BaseCurrency, value,
                last.Value != 0m ? value / last.Value : 0m, decimal.Round((latest?.NetContributions ?? 0m) * rate, 2),
                latest is null || startIndex == 0m ? null : latest.TwrIndex / startIndex - 1m);
        }).ToList();

        return summary with { AccountId = null, Currency = target, Accounts = rows, Reports = [.. reports.Where(report => report.Date >= start)] };
    }

    private sealed record AccountHistory(IReadOnlyList<Snapshot> Series, IReadOnlyList<Snapshot> Subs, IReadOnlyDictionary<DateOnly, PlanPoint>? Plan, IReadOnlyList<ReportMarkerDto> Reports);

    private async Task<AccountHistory> HistoryAsync(AccountSetupDto account, CancellationToken cancellationToken)
    {
        await builder.EnsureFreshAsync(account.Id, cancellationToken);
        var all = await store.SnapshotsAsync(account.Id, SnapshotScenario.Actual, DateOnly.MinValue, cancellationToken);
        var planned = await store.PlannedReportsAsync(account.Id, cancellationToken);

        var plan = planned.Any(report => report.CountsForPlan)
            ? (await store.SnapshotsAsync(account.Id, SnapshotScenario.FullPlan, DateOnly.MinValue, cancellationToken)).Where(s => s.SubPortfolioId is null).ToDictionary(s => s.Date, s => new PlanPoint(s.Value, s.Flow, s.NetContributions))
            : null;
        var reports = planned.Where(report => !report.IsSuperseded).OrderBy(report => report.MarketAsOf)
            .Select(report => new ReportMarkerDto(report.ReportId, report.MarketAsOf, report.Kind, report.Period, account.Id)).ToList();
        return new AccountHistory(
            [.. all.Where(s => s.SubPortfolioId is null).OrderBy(s => s.Date).Select(ToSnapshot)],
            [.. all.Where(s => s.SubPortfolioId is not null).Select(ToSnapshot)],
            plan,
            reports);
    }

    private static Snapshot ToSnapshot(ValuationSnapshot s) => new(s.Date, s.SubPortfolioId, s.Value, s.Cash, s.Flow, s.NetContributions, s.TwrIndex, s.CostBasis);

    private static (IReadOnlyList<Snapshot> Series, IReadOnlyDictionary<DateOnly, PlanPoint>? Plan) Combine(
        IReadOnlyList<(AccountSetupDto Account, AccountHistory History)> histories, IReadOnlyList<DateOnly> days, FxOn fx)
    {
        var anyPlan = histories.Any(entry => entry.History.Plan is not null);
        var cursors = histories.Select(_ => -1).ToArray();
        var lastPlan = new decimal[histories.Count];
        var series = new List<Snapshot>();
        var plan = new Dictionary<DateOnly, PlanPoint>();
        decimal previous = 0m, index = 1m, contributions = 0m, planContributions = 0m;

        foreach (var day in days)
        {
            decimal value = 0m, cash = 0m, costBasis = 0m, flow = 0m, planned = 0m, planFlow = 0m;

            for (var i = 0; i < histories.Count; i++)
            {
                var (account, history) = histories[i];

                while (cursors[i] + 1 < history.Series.Count && history.Series[cursors[i] + 1].Date <= day)
                    cursors[i]++;

                if (cursors[i] < 0)
                    continue;

                var snapshot = history.Series[cursors[i]];
                var rate = fx(account.BaseCurrency, day) ?? 0m;
                value += snapshot.Value * rate;
                cash += snapshot.Cash * rate;
                costBasis += snapshot.CostBasis * rate;

                if (snapshot.Date == day)
                    flow += snapshot.Flow * rate;

                if (history.Plan?.TryGetValue(snapshot.Date, out var shadow) == true)
                {
                    lastPlan[i] = shadow.Value;

                    if (snapshot.Date == day)
                        planFlow += shadow.Flow * rate;
                }
                else if (history.Plan is null && snapshot.Date == day)
                    planFlow += snapshot.Flow * rate;

                planned += (history.Plan is null ? snapshot.Value : lastPlan[i]) * rate;
            }

            var start = previous + flow;
            if (start > 0m)
                index *= value / start;

            contributions += flow;
            planContributions += planFlow;
            previous = value;
            series.Add(new Snapshot(day, null, decimal.Round(value, 2), decimal.Round(cash, 2), decimal.Round(flow, 2), decimal.Round(contributions, 2), decimal.Round(index, 8), decimal.Round(costBasis, 2)));

            if (anyPlan)
                plan[day] = new PlanPoint(decimal.Round(planned, 2), decimal.Round(planFlow, 2), decimal.Round(planContributions, 2));
        }

        return (series, anyPlan ? plan : null);
    }

    private static (DashboardDto Summary, DateOnly Start, Snapshot? Before) Summarize(IReadOnlyList<Snapshot> history, IReadOnlyDictionary<DateOnly, PlanPoint>? plan, string key)
    {
        var last = history[^1];
        var start = PeriodStart(key, last.Date);
        var window = history.Where(s => s.Date >= start).ToList();
        var before = history.LastOrDefault(s => s.Date < start);
        var baseIndex = before?.TwrIndex ?? 1m;
        var periodTwr = baseIndex > 0m ? last.TwrIndex / baseIndex - 1m : (decimal?)null;
        var years = (last.Date.DayNumber - (before?.Date ?? history[0].Date).DayNumber) / 365.25m;
        decimal? annualized = periodTwr is { } twr && years >= 1m ? (decimal)Math.Pow((double)(1m + twr), (double)(1m / years)) - 1m : null;

        var drawdown = DrawdownCalculator.Of([.. window.Select(s => (s.Date, s.TwrIndex))]);
        var peak = 0m;
        var series = window.Select(s =>
        {
            peak = Math.Max(peak, s.TwrIndex);
            var planned = plan?.GetValueOrDefault(s.Date);

            return new SeriesPointDto(s.Date, s.Value, s.NetContributions, s.TwrIndex / baseIndex, planned?.Value, planned?.NetContributions,
                peak > 0m ? decimal.Round(s.TwrIndex / peak - 1m, 6) : 0m, s.Value - s.Cash, s.CostBasis);
        }).ToList();

        var flows = window.Where(s => s.Flow != 0m).Select(s => (s.Date, -s.Flow)).ToList();

        if (before is not null && before.Value != 0m)
            flows.Insert(0, (before.Date, -before.Value));

        flows.Add((last.Date, last.Value));
        var xirr = XirrCalculator.Of(flows);

        var summary = new DashboardDto(
            null, "", last.Date, key, last.Value, last.NetContributions, last.Value - last.NetContributions,
            periodTwr, annualized, xirr, drawdown.Max, drawdown.Current, plan?.GetValueOrDefault(last.Date)?.Value,
            series, [], [], [], last.Value - last.Cash, last.CostBasis);
        return (summary, start, before);
    }

    private static DashboardDto Empty(Guid? accountId, string currency, string key, IReadOnlyList<ReportMarkerDto> reports) =>
        new(accountId, currency, null, key, 0m, 0m, 0m, null, null, null, 0m, 0m, null, [], [], [], reports, 0m, 0m);

    private static string PeriodKey(string? period) => string.IsNullOrWhiteSpace(period) ? "ALL" : period.ToUpperInvariant();

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
