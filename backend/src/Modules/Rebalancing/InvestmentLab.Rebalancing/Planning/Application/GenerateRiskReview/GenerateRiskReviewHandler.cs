using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;

public sealed record GenerateRiskReviewCommand(Guid AccountId, DateOnly? AsOf = null, ReportScope? Scope = null);

public sealed class GenerateRiskReviewHandler(PlanningInputs inputs, IReportRepository reports, SaleTaxes taxes, TimeProvider clock)
{
    public async Task<Result<IReadOnlyDictionary<Guid, EvaluateStrategyRequest>>> StrategyRequestsAsync(
        GenerateRiskReviewCommand command, CancellationToken cancellationToken)
    {
        var asOf = command.AsOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        if (await inputs.AccountAsync(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");

        var planned = PlannedOf(account, command);
        if (planned.Count == 0)
            return ReportErrors.NoActiveSubPortfolios(account.Id);

        var snapshots = (await inputs.HoldingsAsync(account, cancellationToken)).SubPortfolios.ToDictionary(sub => sub.SubPortfolioId);

        return planned
            .Where(sub => snapshots.GetValueOrDefault(sub.Id) is { Positions.Count: > 0 })
            .ToDictionary(sub => sub.Id, sub => new EvaluateStrategyRequest(
                sub.StrategyId!.Value, sub.PinnedStrategyVersion, PlanningInputs.StateOf(account, snapshots[sub.Id]), 0m, asOf, sub.CashInstrumentId));
    }

    public async Task<Result<ReportId>> Handle(
        GenerateRiskReviewCommand command, IReadOnlyDictionary<Guid, StrategyEvaluationResult<RiskAssessmentDto>> assessments,
        ReportRequest request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var asOf = command.AsOf ?? DateOnly.FromDateTime(now.UtcDateTime);

        if (await inputs.AccountAsync(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");

        var planned = PlannedOf(account, command);
        if (planned.Count == 0)
            return ReportErrors.NoActiveSubPortfolios(account.Id);

        var code = command.Scope?.Code ?? ReportDefaults.CodeOf(ReportKind.RiskReview);
        var title = command.Scope?.Title ?? ReportDefaults.TitleOf(ReportKind.RiskReview);

        var currency = Currency.Of(account.BaseCurrency);
        var holdings = await inputs.HoldingsAsync(account, cancellationToken);
        var labels = await inputs.LabelsAsync(cancellationToken);
        string Label(Guid id) => labels.TryGetValue(id, out var label) ? label : id.ToString();
        var snapshots = holdings.SubPortfolios.ToDictionary(sub => sub.SubPortfolioId);
        var heldIds = holdings.SubPortfolios.Where(sub => planned.Any(each => each.Id == sub.SubPortfolioId))
            .SelectMany(sub => sub.Positions.Select(position => position.InstrumentId)).ToList();
        var warnings = new List<string>();
        var prices = await inputs.PricesAsync(heldIds, asOf, account.BaseCurrency, warnings, cancellationToken);
        warnings.InsertRange(0, PlanningInputs.PriceWarnings(heldIds, prices, labels));

        if (request.Cause is { } cause)
            warnings.Insert(0, cause);

        var outcomes = new List<SubPortfolioOutcome>();

        foreach (var sub in planned)
        {
            var snapshot = snapshots.GetValueOrDefault(sub.Id);
            if (snapshot is null || snapshot.Positions.Count == 0)
                continue;

            var result = assessments.GetValueOrDefault(sub.Id) ?? new(null, "strategy.no_answer", "стратегія не відповіла");
            if (!result.IsSuccess)
            {
                warnings.Add($"Оцінка ризику під-портфеля «{sub.Name}» не вдалася: {result.ErrorMessage}.");
                outcomes.Add(new SubPortfolioOutcome(sub.Id, sub.Name, sub.TargetShare, 0m, null, null, null, [], [], Error: result.ErrorMessage));
                continue;
            }

            var assessment = result.Value!;
            var orders = new List<PlannedOrder>();
            var notes = new List<string>();

            foreach (var signal in assessment.Signals)
            {
                var position = snapshot.Positions.FirstOrDefault(each => each.InstrumentId == signal.InstrumentId);
                if (signal.Kind == "exit" && position is not null && prices.TryGetValue(signal.InstrumentId, out var price))
                    orders.Add(new PlannedOrder(OrderKind.Exit, signal.InstrumentId, position.Quantity, price.Close, position.Quantity * price.Close, $"сигнал виходу: {signal.Reason}"));
                else
                    notes.Add($"{Label(signal.InstrumentId)}: {signal.Reason}");
            }

            outcomes.Add(new SubPortfolioOutcome(
                sub.Id, sub.Name, sub.TargetShare, 0m,
                new StrategyInfo(assessment.StrategyId, assessment.StrategyVersion, assessment.StrategyCode, assessment.EngineVersion),
                null,
                null,
                orders,
                notes,
                new SectionRisk(
                    assessment.Status,
                    assessment.MarketAlerts,
                    [.. assessment.Positions.Select(position => new SectionRiskPosition(
                        position.InstrumentId, position.Status, position.Quantity, position.AverageCost, position.CurrentPrice, position.Value,
                        position.PnlPct, position.DrawdownFromHigh, position.ExitPrice, position.ExitRule, position.ExitDistance, position.Alerts, position.Sell))],
                    assessment.Portfolio.Alerts,
                    assessment.Actions)));
        }

        var marketAsOf = prices.Count > 0 ? prices.Values.Max(price => price.Date) : asOf;
        var draft = ReportComposer.Compose(
            new ReportHeader(account.Id, UserId.Of(account.OwnerId), account.Name, currency, YearMonth.Containing(asOf), ReportKind.RiskReview, request.Trigger, marketAsOf, code, title),
            null, outcomes, warnings);

        var report = RebalancingReport.Generate(draft, now);
        await taxes.EstimateAsync(report, asOf, cancellationToken);
        reports.Add(report);

        foreach (var open in (await reports.OpenAsync(account.Id, cancellationToken)).Where(each => each.Kind == ReportKind.RiskReview && each.Code == code))
            open.SupersedeBy(report.Id, now);

        report.Generated(request.RequestedAt, now);
        request.Complete(report.Id, now);
        await reports.SaveChangesAsync(cancellationToken);

        return report.Id;
    }

    private static List<Portfolio.Contracts.Accounts.SubPortfolioSetupDto> PlannedOf(Portfolio.Contracts.Accounts.AccountSetupDto account, GenerateRiskReviewCommand command) =>
        [.. PlanningInputs.Planned(account).Where(sub => command.Scope?.SubPortfolioIds.Contains(sub.Id) ?? true)];
}
