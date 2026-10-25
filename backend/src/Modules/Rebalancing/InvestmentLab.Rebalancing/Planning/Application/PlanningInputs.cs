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

/// <summary>What planning reads from other modules for one account: setup, holdings, instrument labels and prices.</summary>
public sealed class PlanningInputs(IAccountSetupQueries accounts, IHoldingsQueries holdings, IInstrumentCatalog instruments, IQuoteHistory quotes)
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

    public async Task<IReadOnlyDictionary<Guid, LatestPriceDto>> PricesAsync(IEnumerable<Guid> instrumentIds, DateOnly asOf, CancellationToken cancellationToken)
    {
        var ids = instrumentIds.Distinct().ToList();
        return ids.Count == 0
            ? new Dictionary<Guid, LatestPriceDto>()
            : (await quotes.GetLatestPrices(ids, asOf, cancellationToken)).ToDictionary(price => price.InstrumentId);
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
