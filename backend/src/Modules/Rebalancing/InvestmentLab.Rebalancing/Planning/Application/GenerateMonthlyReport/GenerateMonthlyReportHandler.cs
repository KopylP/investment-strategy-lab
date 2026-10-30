using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Reviews.Domain;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Taxation.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;

/// <param name="Contribution">Null: the monthly contribution from the plan settings, minus what was already paid in this period.</param>
/// <param name="AsOf">Null: today. Quotes up to this date are used.</param>
/// <param name="Scope">Null: every planned sub-portfolio, as the "rebalance" report.</param>
public sealed record GenerateMonthlyReportCommand(Guid AccountId, decimal? Contribution = null, DateOnly? AsOf = null, ReportScope? Scope = null);

/// <summary>
/// A rebalance report (§7.1) — a strategy's report in "rebalance" mode — on schedule or on demand (D14): contribution
/// split → strategy per sub-portfolio → guardrails → rebalancer → netting → report. It covers the sub-portfolios of its
/// scope and their share of the monthly contribution. An open report of the same code and period is superseded by the
/// new one; marks already made stay in the journal, so the new plan starts from what is really held.
/// </summary>
public sealed class GenerateMonthlyReportHandler(
    PlanningInputs inputs,
    IStrategyEvaluation strategies,
    IPlanSettingsRepository settings,
    IReportRepository reports,
    IPlanReviewer reviewer,
    SaleTaxes taxes,
    TimeProvider clock)
{
    public async Task<Result<ReportId>> Handle(GenerateMonthlyReportCommand command, ReportTrigger trigger, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var asOf = command.AsOf ?? DateOnly.FromDateTime(now.UtcDateTime);
        var period = YearMonth.Containing(asOf);

        if (await inputs.AccountAsync(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");
        var all = PlanningInputs.Planned(account).ToList();
        var planned = command.Scope is { } scope ? all.Where(sub => scope.SubPortfolioIds.Contains(sub.Id)).ToList() : all;
        if (planned.Count == 0)
            return ReportErrors.NoActiveSubPortfolios(account.Id);
        if (command.Contribution is < 0m)
            return ReportErrors.ContributionNegative();
        var code = command.Scope?.Code ?? ReportDefaults.CodeOf(ReportKind.Monthly);
        var title = command.Scope?.Title ?? ReportDefaults.TitleOf(ReportKind.Monthly);

        // The scope's part of the monthly contribution (by target shares), less what reports of this code already paid in.
        var planSettings = await settings.FindAsync(account.Id, cancellationToken) ?? PlanSettings.DefaultFor(account.Id, UserId.Of(account.OwnerId));
        var periodReports = await reports.ForPeriodAsync(account.Id, period, cancellationToken);
        var contributed = periodReports.Where(report => report.Code == code).Sum(report => report.ContributedAmount);
        var allShares = all.Sum(sub => sub.TargetShare);
        var part = allShares > 0m ? planned.Sum(sub => sub.TargetShare) / allShares : 1m;
        var contribution = command.Contribution ?? Math.Max(0m, decimal.Round(planSettings.MonthlyContribution * part, 2) - contributed);

        var currency = Currency.Of(account.BaseCurrency);
        var holdings = await inputs.HoldingsAsync(account, cancellationToken);
        var labels = await inputs.LabelsAsync(cancellationToken);
        string Label(Guid id) => labels.TryGetValue(id, out var label) ? label : id.ToString();
        var snapshots = holdings.SubPortfolios.ToDictionary(sub => sub.SubPortfolioId);

        var heldIds = holdings.SubPortfolios.SelectMany(sub => sub.Positions.Select(position => position.InstrumentId));
        var cashIds = planned.Select(sub => sub.CashInstrumentId).OfType<Guid>();
        var warnings = new List<string>();
        var prices = new Dictionary<Guid, MarketData.Contracts.Quotes.LatestPriceDto>(
            await inputs.PricesAsync(heldIds.Concat(cashIds), asOf, account.BaseCurrency, warnings, cancellationToken));

        // 1. Contribution split by how far each sub-portfolio is from its target share.
        var standings = planned.Select(sub => new SubPortfolioStanding(sub.Id, Percentage.Of(sub.TargetShare), ValueOf(snapshots.GetValueOrDefault(sub.Id), prices))).ToList();
        var split = Enum.TryParse<ContributionSplit>(account.ContributionPolicy, out var policy) ? policy : ContributionSplit.FillUnderweight;
        var allocation = ContributionAllocator.Allocate(contribution, standings, split, Percentage.Of(account.DriftBand));

        // 2. Each strategy decides for its sub-portfolio.
        var decisions = new Dictionary<Guid, StrategyEvaluationResult<StrategyDecisionDto>>();
        foreach (var sub in planned)
        {
            var request = new EvaluateStrategyRequest(
                sub.StrategyId!.Value, sub.PinnedStrategyVersion, PlanningInputs.StateOf(account, snapshots.GetValueOrDefault(sub.Id)),
                allocation.For(sub.Id), asOf, sub.CashInstrumentId);
            decisions[sub.Id] = await strategies.EvaluateAsync(request, cancellationToken);
        }

        var targetIds = decisions.Values.Where(result => result.IsSuccess)
            .SelectMany(result => result.Value!.Targets.Select(target => target.InstrumentId)
                .Concat(result.Value.CashInstrumentId is { } cash ? [cash] : []))
            .Where(id => !prices.ContainsKey(id));
        foreach (var (id, price) in await inputs.PricesAsync(targetIds, asOf, account.BaseCurrency, warnings, cancellationToken))
            prices[id] = price;

        // 3. Guardrails, then the rebalancer per sub-portfolio.
        var outcomes = new List<SubPortfolioOutcome>();
        foreach (var sub in planned)
        {
            var snapshot = snapshots.GetValueOrDefault(sub.Id);
            var share = allocation.For(sub.Id);
            var result = decisions[sub.Id];
            if (!result.IsSuccess)
            {
                warnings.Add($"Стратегія під-портфеля «{sub.Name}» не відповіла: {result.ErrorMessage}. Його позиції цього разу не змінюються.");
                outcomes.Add(new SubPortfolioOutcome(sub.Id, sub.Name, sub.TargetShare, share, null, null, null, [], [], Error: result.ErrorMessage));
                continue;
            }

            var decision = result.Value!;
            var (targets, clipped) = GuardrailEnforcer.Clip(PlanningInputs.TargetsOf(decision), PlanningInputs.LimitsOf(sub), Label);
            var plan = SubPortfolioRebalancer.Plan(new SubPortfolioPlanInput(
                targets,
                [.. (snapshot?.Positions ?? []).Select(position => new SubPortfolioPosition(position.InstrumentId, Quantity.Of(position.Quantity), position.AverageCost))],
                snapshot?.Cash ?? 0m,
                share,
                prices.ToDictionary(price => price.Key, price => price.Value.Close),
                decision.CashInstrumentId,
                planSettings.Policy,
                currency,
                labels));

            List<string> notes = [.. clipped];
            if (GuardrailEnforcer.CheckTurnover(plan, PlanningInputs.LimitsOf(sub)) is { } turnover)
                notes.Add(turnover);
            warnings.AddRange(notes.Select(note => $"«{sub.Name}»: {note}"));

            outcomes.Add(new SubPortfolioOutcome(
                sub.Id, sub.Name, sub.TargetShare, share,
                new StrategyInfo(decision.StrategyId, decision.StrategyVersion, decision.StrategyCode, decision.EngineVersion),
                PlanningInputs.DecisionOf(decision, []),
                plan,
                plan.Orders,
                notes));
        }

        if (outcomes.All(outcome => outcome.Error is not null))
            return ReportErrors.NoStrategyAnswered(string.Join("; ", outcomes.Select(outcome => outcome.Error).Distinct()));

        var needed = outcomes.SelectMany(outcome => outcome.Orders.Select(order => order.InstrumentId))
            .Concat(holdings.SubPortfolios.SelectMany(sub => sub.Positions.Select(position => position.InstrumentId)));
        warnings.InsertRange(0, PlanningInputs.PriceWarnings(needed, prices, labels));
        if (allocation.Transfer is { } transfer)
            warnings.Add($"Під-портфелі відхилилися від цільових часток більше ніж на {Percentage.Of(account.DriftBand)}: " +
                         $"варто перенести {transfer.Amount:0.00} {currency} з «{NameOf(account, transfer.FromSubPortfolioId)}» до «{NameOf(account, transfer.ToSubPortfolioId)}».");

        var marketAsOf = prices.Count > 0 ? prices.Values.Max(price => price.Date) : asOf;
        var draft = ReportComposer.Compose(
            new ReportHeader(account.Id, UserId.Of(account.OwnerId), account.Name, currency, period, ReportKind.Monthly, trigger, marketAsOf, code, title),
            allocation, outcomes, warnings);
        draft = draft with { Review = await reviewer.ReviewAsync(draft, cancellationToken) };

        var report = RebalancingReport.Generate(draft, now);
        await taxes.EstimateAsync(report, asOf, cancellationToken);
        reports.Add(report);
        foreach (var open in periodReports.Where(each => each.IsOpen && each.Code == code && each.Kind is ReportKind.Monthly or ReportKind.Recalculation))
        {
            open.SupersedeBy(report.Id, now);
            reports.Publish(new ReportSupersededV1(open.Id.Value, open.AccountId, report.Id.Value, now), now);
        }
        reports.Publish(report.ToGeneratedEvent(), now);
        await reports.SaveChangesAsync(cancellationToken);
        return report.Id;
    }

    private static decimal ValueOf(Portfolio.Contracts.Holdings.SubPortfolioHoldingsDto? snapshot, IReadOnlyDictionary<Guid, MarketData.Contracts.Quotes.LatestPriceDto> prices) =>
        snapshot is null
            ? 0m
            : snapshot.Cash + snapshot.Positions.Sum(position =>
                position.Quantity * (prices.TryGetValue(position.InstrumentId, out var price) ? price.Close : position.AverageCost));

    private static string NameOf(AccountSetupDto account, Guid subPortfolioId) =>
        account.SubPortfolios.FirstOrDefault(sub => sub.Id == subPortfolioId)?.Name ?? subPortfolioId.ToString();
}

internal static class ReportEventMapping
{
    extension(RebalancingReport report)
    {
        public ReportGeneratedV1 ToGeneratedEvent() => new(
            report.Id.Value,
            report.AccountId,
            report.OwnerId.Value,
            report.Period.ToString(),
            report.Kind.ToString(),
            report.MarketAsOf,
            report.Currency.Code,
            report.GeneratedAt,
            [.. report.Actions.Select(action => new PlannedActionDto(
                action.Id.Value,
                action.Kind.ToString(),
                action.InstrumentId,
                action.PlannedQuantity,
                action.PlannedPrice,
                action.PlannedAmount,
                [.. action.Allocations.Select(allocation => new PlannedAllocationDto(allocation.SubPortfolioId, allocation.Quantity))],
                action.FromSubPortfolioId))],
            report.AccountName,
            report.Trigger.ToString(),
            report.Title);
    }
}
