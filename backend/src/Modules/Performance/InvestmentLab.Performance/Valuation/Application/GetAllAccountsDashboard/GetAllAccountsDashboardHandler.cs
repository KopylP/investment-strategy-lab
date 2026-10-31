using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Performance.Valuation.Application.GetAllAccountsDashboard;

public sealed class GetAllAccountsDashboardHandler(AccountHistoryLoader histories, IAccountSetupQueries accounts, PriceBook prices)
{
    private const string DefaultCurrency = "EUR";
    private const int CurrencyCodeLength = 3;

    public async Task<Result<DashboardDto>> Handle(string? currency, string? period, CancellationToken cancellationToken)
    {
        var setups = (await accounts.GetAccountSetups(cancellationToken)).OrderBy(account => account.Name).ToList();
        var key = DashboardPeriod.KeyOf(period);

        var target = TargetCurrency(currency, setups);
        if (target.Length != CurrencyCodeLength)
            return Error.Validation("dashboard.currency_invalid", $"{currency} is not a currency code.");

        var all = await LoadAllAsync(setups, cancellationToken);
        var reports = all.SelectMany(entry => entry.History.Reports).OrderBy(report => report.Date).ToList();

        var withHistory = all.Where(entry => entry.History.Series.Count > 0).ToList();
        if (withHistory.Count == 0)
            return DashboardSummary.Empty(null, target, key, reports) with { Accounts = [.. setups.Select(EmptyRowOf)] };

        var days = withHistory.SelectMany(entry => entry.History.Series.Select(snapshot => snapshot.Date)).Distinct().Order().ToList();
        string[] currencies = [.. withHistory.Select(entry => entry.Account.BaseCurrency)];
        var fx = await prices.LoadFxAsync(currencies, target, days[0], days[^1], cancellationToken);

        var (series, plan) = CombinedAccountHistory.Of(withHistory, days, fx);
        var summary = DashboardSummary.Of(series, plan, key);

        return summary.Dashboard with
        {
            AccountId = null,
            Currency = target,
            Accounts = [.. all.Select(entry => RowOf(entry, series[^1], summary.Start, fx))],
            Reports = [.. reports.Where(report => report.Date >= summary.Start)],
        };
    }

    private static string TargetCurrency(string? currency, List<AccountSetupDto> setups) =>
        string.IsNullOrWhiteSpace(currency) ? setups.FirstOrDefault()?.BaseCurrency ?? DefaultCurrency : currency.ToUpperInvariant();

    private async Task<List<AccountWithHistory>> LoadAllAsync(List<AccountSetupDto> setups, CancellationToken cancellationToken)
    {
        var all = new List<AccountWithHistory>();

        foreach (var setup in setups)
            all.Add(new AccountWithHistory(setup, await histories.LoadAsync(setup, cancellationToken)));

        return all;
    }

    private static AccountPerformanceDto EmptyRowOf(AccountSetupDto setup) =>
        new(setup.Id, setup.Name, setup.Broker, setup.BaseCurrency, Value: 0m, Share: 0m, NetContributions: 0m, Twr: null);

    private static AccountPerformanceDto RowOf(AccountWithHistory entry, Snapshot total, DateOnly start, FxOn fx)
    {
        var (account, history) = entry;
        var latest = history.Series.LastOrDefault(snapshot => snapshot.Date <= total.Date);
        var rate = latest is null ? 0m : fx(account.BaseCurrency, total.Date) ?? 0m;
        var startIndex = history.Series.LastOrDefault(snapshot => snapshot.Date < start)?.TwrIndex ?? 1m;
        var value = decimal.Round((latest?.Value ?? 0m) * rate, 2);

        return new AccountPerformanceDto(
            Id: account.Id,
            Name: account.Name,
            Broker: account.Broker,
            BaseCurrency: account.BaseCurrency,
            Value: value,
            Share: total.Value != 0m ? value / total.Value : 0m,
            NetContributions: decimal.Round((latest?.NetContributions ?? 0m) * rate, 2),
            Twr: latest is null || startIndex == 0m ? null : latest.TwrIndex / startIndex - 1m);
    }
}
