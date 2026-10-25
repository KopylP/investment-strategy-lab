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

public sealed record SubPortfolioCadence(Guid SubPortfolioId, string SubPortfolioName, string StrategyName, ReportCadence Cadence);

/// <summary>What planning reads from other modules for one account: setup, holdings, instrument labels and prices.</summary>
public sealed class PlanningInputs(
    IAccountSetupQueries accounts, IHoldingsQueries holdings, IInstrumentCatalog instruments, IQuoteHistory quotes, IFxRates fxRates, IStrategyDirectory strategies)
{
    private const string YahooPrefix = "yfinance:";

    public Task<AccountSetupDto?> AccountAsync(Guid accountId, CancellationToken cancellationToken) => accounts.GetAccountSetup(accountId, cancellationToken);

    public Task<IReadOnlyList<AccountSetupDto>> AccountsAsync(CancellationToken cancellationToken) => accounts.GetAccountSetups(cancellationToken);

    public async Task<AccountHoldingsDto> HoldingsAsync(AccountSetupDto account, CancellationToken cancellationToken) =>
        await holdings.GetHoldings(account.Id, null, cancellationToken) ?? new AccountHoldingsDto(account.Id, account.BaseCurrency, null, []);

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

    /// <summary>The reports each planned sub-portfolio's strategy asks for (its "reporting" parameters).</summary>
    public async Task<IReadOnlyList<SubPortfolioCadence>> CadencesAsync(AccountSetupDto account, CancellationToken cancellationToken)
    {
        var directory = (await strategies.GetStrategies(cancellationToken)).ToDictionary(strategy => strategy.Id);
        return [.. Planned(account).Select(sub => directory.TryGetValue(sub.StrategyId!.Value, out var strategy)
            ? new SubPortfolioCadence(sub.Id, sub.Name, strategy.Name, ReportCadence.Of(strategy.Reporting.Rebalance, strategy.Reporting.RiskReview))
            : new SubPortfolioCadence(sub.Id, sub.Name, "", ReportCadence.Classic))];
    }

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
            score.Volatility, score.AboveMovingAverage, score.Rank, score.Status, score.Reason))],
        [.. decision.Warnings, .. extraWarnings]);

    public static StrategyTargets TargetsOf(StrategyDecisionDto decision) => new(
        decision.Market.IsFavorable,
        Percentage.Of(Math.Clamp(decision.InvestFraction, 0m, 1m)),
        [.. decision.Targets.Select(target => new TargetWeight(target.InstrumentId, target.Weight))],
        [.. decision.Rationale.Scores.Select(score => new InstrumentScore(score.InstrumentId, score.Rank, score.Momentum, score.Reason))]);
}
