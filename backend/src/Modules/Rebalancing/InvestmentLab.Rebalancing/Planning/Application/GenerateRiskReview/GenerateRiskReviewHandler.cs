using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;

public sealed class GenerateRiskReviewHandler(
    IPlanningAccounts accounts,
    IAccountHoldings accountHoldings,
    IInstrumentPrices instrumentPrices,
    IInstrumentLabels instrumentLabels,
    IReportRepository reports,
    GeneratedReportSaver saver,
    TimeProvider clock)
{
    public async Task<Result<ReportId>> Handle(
        GenerateRiskReviewCommand command,
        IReadOnlyDictionary<Guid, StrategyAnswer<StrategyRiskAssessment>> assessments,
        ReportRequest request,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var asOf = command.AsOf ?? DateOnly.FromDateTime(now.UtcDateTime);

        if (await accounts.FindAsync(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");

        var planned = RiskReviewStrategyRequests.PlannedOf(account, command);
        if (planned.Count == 0)
            return ReportErrors.NoActiveSubPortfolios(account.Id);

        var holdings = await accountHoldings.OfAsync(account.Id, account.BaseCurrency, cancellationToken);
        var review = await ReviewedMarketAsync(account, holdings, planned, asOf, request, cancellationToken);
        var snapshots = holdings.ToDictionary(sub => sub.Id);
        var outcomes = Outcomes(planned, snapshots, assessments, review);

        var header = HeaderOf(account, command, asOf, request, review);
        var draft = ReportComposer.Compose(header, null, outcomes, review.Warnings);
        var report = RebalancingReport.Generate(draft, now);

        var open = await reports.OpenAsync(account.Id, cancellationToken);
        await saver.SaveAsync(report, open, asOf, now, request, cancellationToken);

        return report.Id;
    }

    private async Task<ReviewedMarket> ReviewedMarketAsync(
        IPlanningAccount account,
        IReadOnlyList<IHeldSubPortfolio> holdings,
        List<IPlanningSubPortfolio> planned,
        DateOnly asOf,
        ReportRequest request,
        CancellationToken cancellationToken)
    {
        var labels = await instrumentLabels.AllAsync(cancellationToken);
        var heldIds = HeldInstruments(holdings, planned);

        var found = await instrumentPrices.InCurrencyAsync(heldIds, asOf, account.BaseCurrency, cancellationToken);
        var prices = found.Prices;

        List<string> warnings = [.. PriceWarnings.For(heldIds, prices, labels), .. found.Warnings];

        if (request.Cause is { } cause)
            warnings.Insert(0, cause);

        return new ReviewedMarket(labels, prices, warnings);
    }

    private static ReportHeader HeaderOf(
        IPlanningAccount account, GenerateRiskReviewCommand command, DateOnly asOf, ReportRequest request, ReviewedMarket review) => new(
        AccountId: account.Id,
        OwnerId: account.OwnerId,
        AccountName: account.Name,
        Currency: Currency.Of(account.BaseCurrency),
        Period: YearMonth.Containing(asOf),
        Kind: ReportKind.RiskReview,
        Trigger: request.Trigger,
        MarketAsOf: ReportHeader.MarketDateOf([.. review.Prices.Values.Select(price => price.Date)], asOf),
        Code: command.Scope.Code,
        Title: command.Scope.Title);

    private static List<Guid> HeldInstruments(IReadOnlyList<IHeldSubPortfolio> holdings, List<IPlanningSubPortfolio> planned) =>
        [.. holdings
            .Where(sub => planned.Any(each => each.Id == sub.Id))
            .SelectMany(sub => sub.Positions.Select(position => position.InstrumentId))];

    private static List<SubPortfolioOutcome> Outcomes(
        List<IPlanningSubPortfolio> planned,
        Dictionary<Guid, IHeldSubPortfolio> snapshots,
        IReadOnlyDictionary<Guid, StrategyAnswer<StrategyRiskAssessment>> assessments,
        ReviewedMarket review)
    {
        var outcomes = new List<SubPortfolioOutcome>();

        foreach (var sub in planned)
        {
            var snapshot = snapshots.GetValueOrDefault(sub.Id);
            if (snapshot is null || snapshot.Positions.Count == 0)
                continue;

            var result = assessments.GetValueOrDefault(sub.Id) ?? StrategyAnswer<StrategyRiskAssessment>.Failed(Message.Of("report.run.no_answer"));

            outcomes.Add(result.IsSuccess
                ? AssessedOutcome(sub, snapshot, result.Value!, review)
                : FailedOutcome(sub, result.ErrorMessage, review));
        }

        return outcomes;
    }

    private static SubPortfolioOutcome FailedOutcome(IPlanningSubPortfolio sub, string? error, ReviewedMarket review)
    {
        review.Warnings.Add(Message.Of("report.warning.risk_failed", new { sub = sub.Name, error }));

        return SubPortfolioOutcome.Failed(sub.Id, sub.Name, sub.TargetShare, 0m, error);
    }

    private static SubPortfolioOutcome AssessedOutcome(
        IPlanningSubPortfolio sub, IHeldSubPortfolio snapshot, StrategyRiskAssessment assessment, ReviewedMarket review)
    {
        var exits = ExitPlan.Of(assessment.Signals, HeldQuantities(snapshot), Closes(review), review.LabelOf);

        return new SubPortfolioOutcome(
            SubPortfolioId: sub.Id,
            Name: sub.Name,
            TargetShare: sub.TargetShare,
            Contribution: 0m,
            Strategy: assessment.Strategy,
            Decision: null,
            Plan: null,
            Orders: exits.Orders,
            Notes: exits.Notes,
            Risk: assessment.Section);
    }

    private static Dictionary<Guid, decimal> HeldQuantities(IHeldSubPortfolio snapshot)
    {
        var quantities = new Dictionary<Guid, decimal>();

        foreach (var position in snapshot.Positions)
            quantities.TryAdd(position.InstrumentId, position.Quantity);

        return quantities;
    }

    private static Dictionary<Guid, decimal> Closes(ReviewedMarket review) =>
        review.Prices.ToDictionary(price => price.Key, price => price.Value.Close);

    private sealed record ReviewedMarket(
        IReadOnlyDictionary<Guid, string> Labels, IReadOnlyDictionary<Guid, IInstrumentPrice> Prices, List<string> Warnings)
    {
        public string LabelOf(Guid instrumentId) => Labels.TryGetValue(instrumentId, out var label) ? label : instrumentId.ToString();
    }
}
