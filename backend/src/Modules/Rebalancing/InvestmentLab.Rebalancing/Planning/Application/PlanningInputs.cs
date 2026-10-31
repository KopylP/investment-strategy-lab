using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application;

public interface IPlanSettingsRepository
{
    Task<PlanSettings?> FindAsync(Guid accountId, CancellationToken cancellationToken);

    void Add(PlanSettings settings);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>One report an account gets: what the strategies of some of its sub-portfolios declare under one code.</summary>
/// <param name="Mode">"rebalance" or "riskReview" (<see cref="StrategyReportDto"/>).</param>
/// <param name="SubPortfolios">Those whose strategy makes it, each with its strategy's default cadence.</param>
/// <param name="Own">The user's own schedule for the account (<see cref="PlanSettings.ScheduleOf"/>); null — each strategy's default.</param>
public sealed record AccountReport(string Code, string Name, string Description, string Mode, IReadOnlyList<SubPortfolioReport> SubPortfolios, OwnReportSchedule? Own = null)
{
    public ReportKind Kind => Mode == StrategyReportDto.RiskReview ? ReportKind.RiskReview : ReportKind.Monthly;

    /// <summary>When the report comes for this sub-portfolio: the user's time if set, else its strategy's.</summary>
    public Cadence CadenceOf(SubPortfolioReport sub) => Own?.Cadence ?? sub.Default;
}

/// <param name="Default">When its strategy's type says the report comes.</param>
public sealed record SubPortfolioReport(Guid SubPortfolioId, string SubPortfolioName, string StrategyName, Cadence Default);

/// <summary>What planning reads from other modules for one account: setup, holdings, instrument labels and prices.</summary>
public sealed class PlanningInputs(
    IAccountSetupQueries accounts, IHoldingsQueries holdings, IInstrumentCatalog instruments, IQuoteHistory quotes, IFxRates fxRates, IStrategyDirectory strategies)
{
    private const string YahooPrefix = "yfinance:";

    public Task<AccountSetupDto?> AccountAsync(Guid accountId, CancellationToken cancellationToken) => accounts.GetAccountSetup(accountId, cancellationToken);

    public Task<IReadOnlyList<AccountSetupDto>> AccountsAsync(CancellationToken cancellationToken) => accounts.GetAccountSetups(cancellationToken);

    /// <summary>
    /// Current holdings with every sub-portfolio's cash in the account currency: balances the broker keeps in other
    /// currencies (USD after selling a US stock) are money to invest too, counted at the latest NBP cross rate.
    /// </summary>
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

    /// <summary>Ticker per instrument (the Yahoo symbol, as in the Python reports), else the name.</summary>
    public async Task<IReadOnlyDictionary<Guid, string>> LabelsAsync(CancellationToken cancellationToken) =>
        (await instruments.GetInstruments(cancellationToken)).ToDictionary(
            instrument => instrument.Id,
            instrument => instrument.ProviderSymbols.FirstOrDefault(symbol => symbol.StartsWith(YahooPrefix, StringComparison.Ordinal)) is { } symbol
                ? symbol[YahooPrefix.Length..]
                : instrument.Name);

    /// <summary>
    /// Latest close per instrument in the account currency. A quote in another currency is converted at the NBP cross
    /// rate (both legs against PLN, last tables up to <paramref name="asOf"/>); without a rate the instrument has no price
    /// and a warning says why.
    /// </summary>
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

    /// <summary>Units of <paramref name="to"/> for one unit of <paramref name="from"/>; NBP publishes both against PLN.</summary>
    private async Task<decimal?> CrossRateAsync(string from, string to, DateOnly asOf, CancellationToken cancellationToken)
    {
        async Task<decimal?> InPln(string currency) => currency == "PLN"
            ? 1m
            : (await fxRates.GetFxRate(currency, "PLN", asOf.AddDays(1), cancellationToken))?.Rate;

        return await InPln(from) is { } fromPln && await InPln(to) is { } toPln && toPln > 0m ? fromPln / toPln : null;
    }

    /// <summary>
    /// The reports the account gets: whatever the strategies of its planned sub-portfolios declare, one per code (in the
    /// order the strategies declare them), with every sub-portfolio whose strategy makes it and the user's own time
    /// from <paramref name="settings"/>.
    /// </summary>
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

    /// <summary>Stale or missing prices make a plan unreliable: the report says so (D14).</summary>
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
