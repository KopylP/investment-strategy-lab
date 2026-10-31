using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;

public sealed record GenerateMonthlyReportCommand(Guid AccountId, decimal? Contribution = null, DateOnly? AsOf = null, ReportScope? Scope = null);

public sealed class GenerateMonthlyReportHandler(
    PlanningInputs inputs,
    IPlanSettingsRepository settings,
    IReportRepository reports,
    IPlanReviewer reviewer,
    SaleTaxes taxes,
    TimeProvider clock)
{
    public async Task<Result<IReadOnlyDictionary<Guid, EvaluateStrategyRequest>>> StrategyRequestsAsync(
        GenerateMonthlyReportCommand command, CancellationToken cancellationToken)
    {
        var prepared = await PrepareAsync(command, cancellationToken);
        if (prepared.IsFailure)
            return prepared.Error;

        var plan = prepared.Value;

        return plan.Planned.ToDictionary(sub => sub.Id, plan.RequestOf);
    }

    public async Task<Result<ReportId>> Handle(
        GenerateMonthlyReportCommand command, IReadOnlyDictionary<Guid, StrategyEvaluationResult<StrategyDecisionDto>> decisions,
        ReportRequest request, CancellationToken cancellationToken)
    {
        var prepared = await PrepareAsync(command, cancellationToken);
        if (prepared.IsFailure)
            return prepared.Error;

        var (account, planned, code, title, period, asOf, now, planSettings, periodReports, currency, holdings, labels, snapshots, prices, warnings, allocation) = prepared.Value;
        string Label(Guid id) => labels.TryGetValue(id, out var label) ? label : id.ToString();

        var targetIds = decisions.Values.Where(result => result.IsSuccess)
            .SelectMany(result => result.Value!.Targets.Select(target => target.InstrumentId)
                .Concat(result.Value.CashInstrumentId is { } cash ? [cash] : []))
            .Where(id => !prices.ContainsKey(id));
        foreach (var (id, price) in await inputs.PricesAsync(targetIds, asOf, account.BaseCurrency, warnings, cancellationToken))
            prices[id] = price;

        var outcomes = new List<SubPortfolioOutcome>();

        foreach (var sub in planned)
        {
            var snapshot = snapshots.GetValueOrDefault(sub.Id);
            var share = allocation.For(sub.Id);
            var result = decisions.GetValueOrDefault(sub.Id) ?? new(null, "strategy.no_answer", "стратегія не відповіла");
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

        if (request.Cause is { } cause)
            warnings.Insert(0, cause);

        if (allocation.Transfer is { } transfer)
            warnings.Add($"Під-портфелі відхилилися від цільових часток більше ніж на {Percentage.Of(account.DriftBand)}: " +
                         $"варто перенести {transfer.Amount:0.00} {currency} з «{NameOf(account, transfer.FromSubPortfolioId)}» до «{NameOf(account, transfer.ToSubPortfolioId)}».");

        var marketAsOf = prices.Count > 0 ? prices.Values.Max(price => price.Date) : asOf;
        var draft = ReportComposer.Compose(
            new ReportHeader(account.Id, UserId.Of(account.OwnerId), account.Name, currency, period, ReportKind.Monthly, request.Trigger, marketAsOf, code, title),
            allocation, outcomes, warnings);
        draft = draft with { Review = await reviewer.ReviewAsync(draft, cancellationToken) };

        var report = RebalancingReport.Generate(draft, now);
        await taxes.EstimateAsync(report, asOf, cancellationToken);
        reports.Add(report);

        foreach (var open in periodReports.Where(each => each.IsOpen && each.Code == code && each.Kind is ReportKind.Monthly or ReportKind.Recalculation))
            open.SupersedeBy(report.Id, now);

        report.Generated(request.RequestedAt, now);
        request.Complete(report.Id, now);
        await reports.SaveChangesAsync(cancellationToken);

        return report.Id;
    }

    private async Task<Result<PreparedPlan>> PrepareAsync(GenerateMonthlyReportCommand command, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var asOf = command.AsOf ?? DateOnly.FromDateTime(now.UtcDateTime);
        var period = YearMonth.Containing(asOf);

        if (await inputs.AccountAsync(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");

        var all = PlanningInputs.Planned(account).ToList();
        var planned = command.Scope is { } scope ? [.. all.Where(sub => scope.SubPortfolioIds.Contains(sub.Id))] : all;
        if (planned.Count == 0)
            return ReportErrors.NoActiveSubPortfolios(account.Id);

        if (command.Contribution is < 0m)
            return ReportErrors.ContributionNegative();

        var code = command.Scope?.Code ?? ReportDefaults.CodeOf(ReportKind.Monthly);
        var title = command.Scope?.Title ?? ReportDefaults.TitleOf(ReportKind.Monthly);

        var planSettings = await settings.FindAsync(account.Id, cancellationToken) ?? PlanSettings.DefaultFor(account.Id, UserId.Of(account.OwnerId));
        var periodReports = await reports.ForPeriodAsync(account.Id, period, cancellationToken);
        var contributed = periodReports.Where(report => report.Code == code).Sum(report => report.ContributedAmount);
        var allShares = all.Sum(sub => sub.TargetShare);
        var part = allShares > 0m ? planned.Sum(sub => sub.TargetShare) / allShares : 1m;
        var contribution = command.Contribution ?? Math.Max(0m, decimal.Round(planSettings.MonthlyContribution * part, 2) - contributed);

        var currency = Currency.Of(account.BaseCurrency);
        var holdings = await inputs.HoldingsAsync(account, cancellationToken);
        var labels = await inputs.LabelsAsync(cancellationToken);
        var snapshots = holdings.SubPortfolios.ToDictionary(sub => sub.SubPortfolioId);

        var heldIds = holdings.SubPortfolios.SelectMany(sub => sub.Positions.Select(position => position.InstrumentId));
        var cashIds = planned.Select(sub => sub.CashInstrumentId).OfType<Guid>();
        var warnings = new List<string>();
        var prices = new Dictionary<Guid, MarketData.Contracts.Quotes.LatestPriceDto>(
            await inputs.PricesAsync(heldIds.Concat(cashIds), asOf, account.BaseCurrency, warnings, cancellationToken));

        var standings = planned.Select(sub => new SubPortfolioStanding(sub.Id, Percentage.Of(sub.TargetShare), ValueOf(snapshots.GetValueOrDefault(sub.Id), prices))).ToList();
        var split = Enum.TryParse<ContributionSplit>(account.ContributionPolicy, out var policy) ? policy : ContributionSplit.FillUnderweight;
        var allocation = ContributionAllocator.Allocate(contribution, standings, split, Percentage.Of(account.DriftBand));

        return new PreparedPlan(
            account, planned, code, title, period, asOf, now, planSettings, periodReports, currency, holdings, labels, snapshots, prices, warnings, allocation);
    }

    private sealed record PreparedPlan(
        AccountSetupDto Account,
        IReadOnlyList<SubPortfolioSetupDto> Planned,
        string Code,
        string Title,
        YearMonth Period,
        DateOnly AsOf,
        DateTimeOffset Now,
        PlanSettings PlanSettings,
        IReadOnlyList<RebalancingReport> PeriodReports,
        Currency Currency,
        Portfolio.Contracts.Holdings.AccountHoldingsDto Holdings,
        IReadOnlyDictionary<Guid, string> Labels,
        Dictionary<Guid, Portfolio.Contracts.Holdings.SubPortfolioHoldingsDto> Snapshots,
        Dictionary<Guid, MarketData.Contracts.Quotes.LatestPriceDto> Prices,
        List<string> Warnings,
        ContributionAllocation Allocation)
    {
        public EvaluateStrategyRequest RequestOf(SubPortfolioSetupDto sub) => new(
            sub.StrategyId!.Value, sub.PinnedStrategyVersion, PlanningInputs.StateOf(Account, Snapshots.GetValueOrDefault(sub.Id)),
            Allocation.For(sub.Id), AsOf, sub.CashInstrumentId);
    }

    private static decimal ValueOf(Portfolio.Contracts.Holdings.SubPortfolioHoldingsDto? snapshot, IReadOnlyDictionary<Guid, MarketData.Contracts.Quotes.LatestPriceDto> prices) =>
        snapshot is null
            ? 0m
            : snapshot.Cash + snapshot.Positions.Sum(position =>
                position.Quantity * (prices.TryGetValue(position.InstrumentId, out var price) ? price.Close : position.AverageCost));

    private static string NameOf(AccountSetupDto account, Guid subPortfolioId) =>
        account.SubPortfolios.FirstOrDefault(sub => sub.Id == subPortfolioId)?.Name ?? subPortfolioId.ToString();
}
