using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

public sealed class PlanningInputs(
    IAccountSetupQueries accounts,
    IHoldingsQueries holdings,
    IInstrumentCatalog instruments,
    IQuoteHistory quotes,
    IFxRates fxRates,
    IStrategyDirectory strategies,
    TimeProvider clock)
{
    public Task<AccountSetupDto?> AccountAsync(
        Guid accountId,
        CancellationToken cancellationToken) => accounts.GetAccountSetup(accountId, cancellationToken);

    public Task<IReadOnlyList<AccountSetupDto>> AccountsAsync(CancellationToken cancellationToken) => accounts.GetAccountSetups(cancellationToken);

    public async Task<AccountHoldingsDto> HoldingsAsync(AccountSetupDto account, CancellationToken cancellationToken)
    {
        var current =
            await holdings.GetHoldings(account.Id, null, cancellationToken) ?? new AccountHoldingsDto(account.Id, account.BaseCurrency, null, []);
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(1);
        var rates = new Dictionary<string, decimal?>();
        var subs = new List<SubPortfolioHoldingsDto>();

        foreach (var sub in current.SubPortfolios)
        {
            var cash = await CashInAccountCurrencyAsync(sub, account.BaseCurrency, today, rates, cancellationToken);

            subs.Add(sub with { Cash = cash });
        }

        return current with { SubPortfolios = subs };
    }

    private async Task<decimal> CashInAccountCurrencyAsync(
        SubPortfolioHoldingsDto sub, string accountCurrency, DateOnly asOf, Dictionary<string, decimal?> rates, CancellationToken cancellationToken)
    {
        var cash = sub.Cash;

        foreach (var foreign in sub.ForeignCash ?? [])
        {
            if (!rates.TryGetValue(foreign.Currency, out var rate))
                rate = rates[foreign.Currency] = await CrossRateAsync(foreign.Currency, accountCurrency, asOf, cancellationToken);

            cash += rate is { } factor ? decimal.Round(foreign.Amount * factor, 2) : 0m;
        }

        return cash;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> LabelsAsync(CancellationToken cancellationToken)
    {
        var known = await instruments.GetInstruments(cancellationToken);

        return known.ToDictionary(instrument => instrument.Id, instrument => instrument.Label);
    }

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
                warnings.Add(ConversionWarning(price.Currency, accountCurrency, rate));
            }

            if (rate is { } factor)
                prices[price.InstrumentId] = Converted(price, factor, accountCurrency);
        }

        return prices;
    }

    private static string ConversionWarning(string currency, string accountCurrency, decimal? rate) => rate is { } known
        ? $"Ціни в {currency} перераховано в {accountCurrency} за курсом NBP {known:0.####}: у брокера сума буде у валюті ETF."
        : $"Немає курсу NBP {currency}/{accountCurrency}: ETF у {currency} цього разу не торгуються. Оновіть курси NBP.";

    private static LatestPriceDto Converted(LatestPriceDto price, decimal factor, string currency) =>
        price with { Close = price.Close * factor, AdjustedClose = price.AdjustedClose * factor, Currency = currency };

    private async Task<decimal?> CrossRateAsync(string from, string to, DateOnly asOf, CancellationToken cancellationToken)
    {
        async Task<decimal?> InPln(string currency) => currency == "PLN"
            ? 1m
            : (await fxRates.GetFxRate(currency, "PLN", asOf.AddDays(1), cancellationToken))?.Rate;

        return await InPln(from) is { } fromPln && await InPln(to) is { } toPln && toPln > 0m ? fromPln / toPln : null;
    }

    public async Task<IReadOnlyList<AccountReport>> ReportsAsync(AccountSetupDto account, PlanSettings? settings, CancellationToken cancellationToken)
    {
        var known = await strategies.GetStrategies(cancellationToken);
        var directory = known.ToDictionary(strategy => strategy.Id);

        var declared = Planned(account)
            .SelectMany(sub => directory.TryGetValue(sub.StrategyId!.Value, out var strategy)
                ? strategy.Reports.Select(report => new DeclaredReport(sub, strategy, report))
                : [])
            .ToList();

        return [.. declared.GroupBy(each => each.Report.Code).Select(group => AccountReportOf([.. group], settings))];
    }

    private static AccountReport AccountReportOf(List<DeclaredReport> declared, PlanSettings? settings)
    {
        var first = declared[0].Report;
        var subPortfolios = declared.Select(each => new SubPortfolioReport(
            each.SubPortfolio.Id, each.SubPortfolio.Name, each.Strategy.Name, CadenceOf(each.Report.Schedule)));

        var kind = first.Mode == StrategyReportDto.RiskReview ? ReportKind.RiskReview : ReportKind.Monthly;

        return new AccountReport(first.Code, first.Name, first.Description, first.Mode, kind, [.. subPortfolios], settings?.ScheduleOf(first.Code));
    }

    private static Cadence CadenceOf(StrategyReportScheduleDto schedule) =>
        Cadence.Of(schedule.Frequency, schedule.Day, schedule.Weekday, schedule.Time, schedule.Minute);

    public static IEnumerable<SubPortfolioSetupDto> Planned(AccountSetupDto account) =>
        account.SubPortfolios.Where(sub => sub.Status == "Active" && sub.StrategyId is not null);

    public static SubPortfolioStateDto StateOf(AccountSetupDto account, SubPortfolioHoldingsDto? snapshot)
    {
        var cash = Math.Max(0m, snapshot?.Cash ?? 0m);
        var positions = snapshot?.Positions ?? [];

        return new SubPortfolioStateDto(account.BaseCurrency, cash, [.. positions.Select(HoldingStateOf)]);
    }

    private static HoldingStateDto HoldingStateOf(PositionDto position)
    {
        var lots = position.Lots.Select(lot => new LotStateDto(lot.Quantity, lot.UnitCost, lot.OpenedOn));

        return new HoldingStateDto(position.InstrumentId, [.. lots]);
    }

    public static GuardrailLimits LimitsOf(SubPortfolioSetupDto sub) => new(
        sub.MaxPositionWeight is { } weight ? Percentage.Of(weight) : null,
        sub.MaxMonthlyTurnover is { } turnover ? Percentage.Of(turnover) : null);

    public static IEnumerable<string> PriceWarnings(
        IEnumerable<Guid> needed,
        IReadOnlyDictionary<Guid, LatestPriceDto> prices,
        IReadOnlyDictionary<Guid, string> labels)
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
        [.. decision.Rationale.Scores.Select(SectionScoreOf)],
        [.. decision.Warnings, .. extraWarnings],
        decision.Rationale.Title,
        decision.Rationale.InstrumentLabel,
        decision.Rationale.Columns?.Select(SectionColumnOf).ToList());

    private static SectionScore SectionScoreOf(InstrumentScoreDto score) => new(
        score.InstrumentId,
        score.Name,
        score.Group,
        score.Price,
        score.Momentum,
        score.Return1m,
        score.Return3m,
        score.Return12m,
        score.Volatility,
        score.AboveMovingAverage,
        score.Rank,
        score.Status,
        score.Reason,
        score.Values);

    private static SectionColumn SectionColumnOf(RationaleColumnDto column) =>
        new(column.Id, column.Label, column.Format, column.Hint, column.Compact, column.TrueHint, column.FalseHint);

    public static StrategyTargets TargetsOf(StrategyDecisionDto decision) => new(
        decision.Market.IsFavorable,
        Percentage.Of(Math.Clamp(decision.InvestFraction, 0m, 1m)),
        [.. decision.Targets.Select(target => new TargetWeight(target.InstrumentId, target.Weight))],
        [.. decision.Rationale.Scores.Select(score => new InstrumentScore(score.InstrumentId, score.Rank, score.Momentum, score.Reason))]);

    private sealed record DeclaredReport(SubPortfolioSetupDto SubPortfolio, StrategySummaryDto Strategy, StrategyReportDto Report);
}
