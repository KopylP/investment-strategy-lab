using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application;

public sealed class PlanningInputs(
    IAccountSetupQueries accounts, IHoldingsQueries holdings, IInstrumentCatalog instruments, IQuoteHistory quotes, IFxRates fxRates, IStrategyDirectory strategies)
{
    private const string YahooPrefix = "yfinance:";

    public Task<AccountSetupDto?> AccountAsync(Guid accountId, CancellationToken cancellationToken) => accounts.GetAccountSetup(accountId, cancellationToken);

    public Task<IReadOnlyList<AccountSetupDto>> AccountsAsync(CancellationToken cancellationToken) => accounts.GetAccountSetups(cancellationToken);

    public async Task<AccountHoldingsDto> HoldingsAsync(AccountSetupDto account, CancellationToken cancellationToken)
    {
        var current = await holdings.GetHoldings(account.Id, null, cancellationToken) ?? new AccountHoldingsDto(account.Id, account.BaseCurrency, null, []);
        var today = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var rates = new Dictionary<string, decimal?>();
        var subs = new List<SubPortfolioHoldingsDto>();

        foreach (var sub in current.SubPortfolios)
        {
            var cash = sub.Cash;

            foreach (var foreign in sub.ForeignCash ?? [])
            {
                if (!rates.TryGetValue(foreign.Currency, out var rate))
                    rate = rates[foreign.Currency] = await CrossRateAsync(foreign.Currency, account.BaseCurrency, today, cancellationToken);

                cash += rate is { } factor ? decimal.Round(foreign.Amount * factor, 2) : 0m;
            }

            subs.Add(sub with { Cash = cash });
        }

        return current with { SubPortfolios = subs };
    }

    public async Task<IReadOnlyDictionary<Guid, string>> LabelsAsync(CancellationToken cancellationToken) =>
        (await instruments.GetInstruments(cancellationToken)).ToDictionary(
            instrument => instrument.Id,
            instrument => instrument.ProviderSymbols.FirstOrDefault(symbol => symbol.StartsWith(YahooPrefix, StringComparison.Ordinal)) is { } symbol
                ? symbol[YahooPrefix.Length..]
                : instrument.Name);

    public async Task<IReadOnlyDictionary<Guid, LatestPriceDto>> PricesAsync(
        IEnumerable<Guid> instrumentIds, DateOnly asOf, string accountCurrency, ICollection<string> warnings, CancellationToken cancellationToken)
    {
        var ids = instrumentIds.Distinct().ToList();
        var prices = new Dictionary<Guid, LatestPriceDto>();

        if (ids.Count == 0)
            return prices;

        var rates = new Dictionary<string, decimal?>();

        foreach (var price in await quotes.GetLatestPrices(ids, asOf, cancellationToken))
        {
            if (string.Equals(price.Currency, accountCurrency, StringComparison.OrdinalIgnoreCase))
            {
                prices[price.InstrumentId] = price;
                continue;
            }

            if (!rates.TryGetValue(price.Currency, out var rate))
            {
                rate = rates[price.Currency] = await CrossRateAsync(price.Currency, accountCurrency, asOf, cancellationToken);
                warnings.Add(rate is { } known
                    ? $"Ціни в {price.Currency} перераховано в {accountCurrency} за курсом NBP {known:0.####}: у брокера сума буде у валюті ETF."
                    : $"Немає курсу NBP {price.Currency}/{accountCurrency}: ETF у {price.Currency} цього разу не торгуються. Оновіть курси NBP.");
            }

            if (rate is { } factor)
                prices[price.InstrumentId] = price with { Close = price.Close * factor, AdjustedClose = price.AdjustedClose * factor, Currency = accountCurrency };
        }

        return prices;
    }

    private async Task<decimal?> CrossRateAsync(string from, string to, DateOnly asOf, CancellationToken cancellationToken)
    {
        async Task<decimal?> InPln(string currency) => currency == "PLN"
            ? 1m
            : (await fxRates.GetFxRate(currency, "PLN", asOf.AddDays(1), cancellationToken))?.Rate;

        return await InPln(from) is { } fromPln && await InPln(to) is { } toPln && toPln > 0m ? fromPln / toPln : null;
    }

    public async Task<IReadOnlyList<AccountReport>> ReportsAsync(AccountSetupDto account, PlanSettings? settings, CancellationToken cancellationToken)
    {
        var directory = (await strategies.GetStrategies(cancellationToken)).ToDictionary(strategy => strategy.Id);
        var declared = Planned(account)
            .SelectMany(sub => directory.TryGetValue(sub.StrategyId!.Value, out var strategy)
                ? strategy.Reports.Select(report => (Sub: sub, Strategy: strategy, Report: report))
                : [])
            .ToList();
        return [.. declared.GroupBy(each => each.Report.Code).Select(group =>
        {
            var first = group.First().Report;

            return new AccountReport(first.Code, first.Name, first.Description, first.Mode, [.. group.Select(each => new SubPortfolioReport(
                each.Sub.Id, each.Sub.Name, each.Strategy.Name, CadenceOf(each.Report.Schedule)))], settings?.ScheduleOf(first.Code));
        })];
    }

    private static Cadence CadenceOf(StrategyReportScheduleDto schedule) =>
        Cadence.Of(schedule.Frequency, schedule.Day, schedule.Weekday, schedule.Time, schedule.Minute);

    public static IEnumerable<SubPortfolioSetupDto> Planned(AccountSetupDto account) =>
        account.SubPortfolios.Where(sub => sub.Status == "Active" && sub.StrategyId is not null);

    public static SubPortfolioStateDto StateOf(AccountSetupDto account, SubPortfolioHoldingsDto? snapshot) => new(
        account.BaseCurrency,
        Math.Max(0m, snapshot?.Cash ?? 0m),
        [.. (snapshot?.Positions ?? []).Select(position => new HoldingStateDto(
            position.InstrumentId,
            [.. position.Lots.Select(lot => new LotStateDto(lot.Quantity, lot.UnitCost, lot.OpenedOn))]))]);

    public static GuardrailLimits LimitsOf(SubPortfolioSetupDto sub) => new(
        sub.MaxPositionWeight is { } weight ? Percentage.Of(weight) : null,
        sub.MaxMonthlyTurnover is { } turnover ? Percentage.Of(turnover) : null);

    public static IEnumerable<string> PriceWarnings(IEnumerable<Guid> needed, IReadOnlyDictionary<Guid, LatestPriceDto> prices, IReadOnlyDictionary<Guid, string> labels)
    {
        string Label(Guid id) => labels.TryGetValue(id, out var label) ? label : id.ToString();

        foreach (var id in needed.Distinct())
        {
            if (!prices.TryGetValue(id, out var price))
                yield return $"Немає котирувань для {Label(id)}: оновіть котирування перед виконанням.";
            else if (price.IsStale)
                yield return $"Котирування {Label(id)} застаріле (останнє {price.Date:yyyy-MM-dd}): оновіть котирування перед виконанням.";
        }
    }

    public static SectionDecision DecisionOf(StrategyDecisionDto decision, IReadOnlyList<string> extraWarnings) => new(
        decision.Market.IsFavorable,
        decision.InvestFraction,
        decision.Market.Reasons,
        decision.Rationale.Summary,
        [.. decision.Targets.Select(target => new SectionTarget(target.InstrumentId, target.Weight))],
        [.. decision.Rationale.Scores.Select(score => new SectionScore(
            score.InstrumentId, score.Name, score.Group, score.Price, score.Momentum, score.Return1m, score.Return3m, score.Return12m,
            score.Volatility, score.AboveMovingAverage, score.Rank, score.Status, score.Reason, score.Values))],
        [.. decision.Warnings, .. extraWarnings],
        decision.Rationale.Title,
        decision.Rationale.InstrumentLabel,
        decision.Rationale.Columns?.Select(column => new SectionColumn(
            column.Id, column.Label, column.Format, column.Hint, column.Compact, column.TrueHint, column.FalseHint)).ToList());

    public static StrategyTargets TargetsOf(StrategyDecisionDto decision) => new(
        decision.Market.IsFavorable,
        Percentage.Of(Math.Clamp(decision.InvestFraction, 0m, 1m)),
        [.. decision.Targets.Select(target => new TargetWeight(target.InstrumentId, target.Weight))],
        [.. decision.Rationale.Scores.Select(score => new InstrumentScore(score.InstrumentId, score.Rank, score.Momentum, score.Reason))]);
}
