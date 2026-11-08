using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;

public sealed class GenerateMonthlyReportHandler(
    MonthlyPlanPreparation preparation,
    IInstrumentPrices instrumentPrices,
    IPlanReviewer reviewer,
    GeneratedReportSaver saver)
{
    public async Task<Result<ReportId>> Handle(
        GenerateMonthlyReportCommand command,
        IReadOnlyDictionary<Guid, StrategyAnswer<StrategyDecision>> decisions,
        ReportRequest request,
        CancellationToken cancellationToken)
    {
        var prepared = await preparation.PrepareAsync(command, cancellationToken);
        if (prepared.IsFailure)
            return prepared.Error;

        var plan = prepared.Value;

        await AddTargetPricesAsync(plan, decisions, cancellationToken);

        var outcomes = plan.Planned.Select(sub => OutcomeOf(sub, plan, decisions)).ToList();
        if (outcomes.All(outcome => outcome.Error is not null))
            return ReportErrors.NoStrategyAnswered(string.Join("; ", outcomes.Select(outcome => outcome.Error).Distinct()));

        AddReportWarnings(plan, outcomes, request);

        var report = await ComposeAsync(plan, outcomes, request, cancellationToken);
        await saver.SaveAsync(report, plan.PeriodReports, plan.AsOf, plan.Now, request, cancellationToken);

        return report.Id;
    }

    private async Task AddTargetPricesAsync(
        MonthlyPlan plan, IReadOnlyDictionary<Guid, StrategyAnswer<StrategyDecision>> decisions, CancellationToken cancellationToken)
    {
        var missingIds = decisions.Values
            .Where(result => result.IsSuccess)
            .SelectMany(result => result.Value!.WantedInstruments)
            .Where(id => !plan.Prices.ContainsKey(id));

        var fetched = await instrumentPrices.InCurrencyAsync(missingIds, plan.AsOf, plan.Account.BaseCurrency, cancellationToken);
        plan.Warnings.AddRange(fetched.Warnings);

        foreach (var (id, price) in fetched.Prices)
            plan.Prices[id] = price;
    }

    private static SubPortfolioOutcome OutcomeOf(
        IPlanningSubPortfolio sub, MonthlyPlan plan, IReadOnlyDictionary<Guid, StrategyAnswer<StrategyDecision>> decisions)
    {
        var share = plan.Allocation.For(sub.Id);
        var result = decisions.GetValueOrDefault(sub.Id) ?? StrategyAnswer<StrategyDecision>.Failed(Message.Of("report.run.no_answer"));

        if (!result.IsSuccess)
        {
            plan.Warnings.Add(Message.Of("report.warning.strategy_no_answer", new { sub = sub.Name, error = result.ErrorMessage }));

            return SubPortfolioOutcome.Failed(sub.Id, sub.Name, sub.TargetShare, share, result.ErrorMessage);
        }

        return RebalancedOutcome(sub, plan, share, result.Value!);
    }

    private static SubPortfolioOutcome RebalancedOutcome(IPlanningSubPortfolio sub, MonthlyPlan plan, decimal share, StrategyDecision decision)
    {
        var limits = GuardrailLimits.Of(sub);
        var (targets, clipped) = GuardrailEnforcer.Clip(decision.Targets, limits, plan.LabelOf);
        var rebalanced = SubPortfolioRebalancer.Plan(SituationOf(sub, plan, share, targets, decision));

        List<string> notes = [.. clipped];

        if (GuardrailEnforcer.CheckTurnover(rebalanced, limits) is { } turnover)
            notes.Add(turnover);

        plan.Warnings.AddRange(notes.Select(note => Message.Of("report.warning.of_sub_portfolio", new { sub = sub.Name, note })));

        return new SubPortfolioOutcome(
            SubPortfolioId: sub.Id,
            Name: sub.Name,
            TargetShare: sub.TargetShare,
            Contribution: share,
            Strategy: decision.Strategy,
            Decision: decision.Section,
            Plan: rebalanced,
            Orders: rebalanced.Orders,
            Notes: notes);
    }

    private static SubPortfolioSituation SituationOf(
        IPlanningSubPortfolio sub, MonthlyPlan plan, decimal share, StrategyTargets targets, StrategyDecision decision)
    {
        var snapshot = plan.Snapshots.GetValueOrDefault(sub.Id);
        var positions = (snapshot?.Positions ?? []).Select(position => PositionInAccountCurrency(position, plan.Prices));

        return new SubPortfolioSituation(
            Strategy: targets,
            Positions: [.. positions],
            Cash: snapshot?.Cash ?? 0m,
            Contribution: share,
            Prices: plan.Prices.ToDictionary(price => price.Key, price => price.Value.Close),
            CashInstrumentId: decision.CashInstrumentId,
            Policy: plan.PlanSettings.Policy,
            Currency: plan.Currency,
            Labels: plan.Labels);
    }

    private static SubPortfolioPosition PositionInAccountCurrency(IHeldPosition position, Dictionary<Guid, IInstrumentPrice> prices)
    {
        var price = prices.GetValueOrDefault(position.InstrumentId);
        var averageCost = price?.FromQuoteCurrency(position.AverageCost);

        return new SubPortfolioPosition(position.InstrumentId, Quantity.Of(position.Quantity), averageCost);
    }

    private static void AddReportWarnings(MonthlyPlan plan, List<SubPortfolioOutcome> outcomes, ReportRequest request)
    {
        var needed = outcomes.SelectMany(outcome => outcome.Orders.Select(order => order.InstrumentId))
            .Concat(plan.Holdings.SelectMany(sub => sub.Positions.Select(position => position.InstrumentId)));

        plan.Warnings.InsertRange(0, PriceWarnings.For(needed, plan.Prices, plan.Labels));

        if (request.Cause is { } cause)
            plan.Warnings.Insert(0, cause);

        if (plan.Allocation.Transfer is { } transfer)
            plan.Warnings.Add(TransferWarning(plan, transfer));
    }

    private static string TransferWarning(MonthlyPlan plan, TransferSuggestion transfer)
    {
        var from = NameOf(plan.Account, transfer.FromSubPortfolioId);
        var to = NameOf(plan.Account, transfer.ToSubPortfolioId);

        return transfer.WarningFor(from, to, Percentage.Of(plan.Account.DriftBand), plan.Currency);
    }

    private static string NameOf(IPlanningAccount account, Guid subPortfolioId) =>
        account.SubPortfolios.FirstOrDefault(sub => sub.Id == subPortfolioId)?.Name ?? subPortfolioId.ToString();

    private async Task<RebalancingReport> ComposeAsync(
        MonthlyPlan plan, List<SubPortfolioOutcome> outcomes, ReportRequest request, CancellationToken cancellationToken)
    {
        var marketAsOf = ReportHeader.MarketDateOf([.. plan.Prices.Values.Select(price => price.Date)], plan.AsOf);
        var header = new ReportHeader(
            AccountId: plan.Account.Id,
            OwnerId: plan.Account.OwnerId,
            AccountName: plan.Account.Name,
            Currency: plan.Currency,
            Period: plan.Period,
            Kind: ReportKind.Monthly,
            Trigger: request.Trigger,
            MarketAsOf: marketAsOf,
            Code: plan.Code,
            Title: plan.Title);

        var draft = ReportComposer.Compose(header, plan.Allocation, outcomes, plan.Warnings);
        draft = draft with { Review = await reviewer.ReviewAsync(draft, cancellationToken) };

        return RebalancingReport.Generate(draft, plan.Now);
    }
}
