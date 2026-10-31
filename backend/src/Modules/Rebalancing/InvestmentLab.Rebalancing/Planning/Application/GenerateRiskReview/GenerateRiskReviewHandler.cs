using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;

public sealed class GenerateRiskReviewHandler(PlanningInputs inputs, IReportRepository reports, SaleTaxes taxes, TimeProvider clock)
{
    public async Task<Result<ReportId>> Handle(
        GenerateRiskReviewCommand command,
        IReadOnlyDictionary<Guid, StrategyEvaluationResult<RiskAssessmentDto>> assessments,
        ReportRequest request,
        CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var asOf = command.AsOf ?? DateOnly.FromDateTime(now.UtcDateTime);

        if (await inputs.AccountAsync(command.AccountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {command.AccountId} does not exist.");

        var planned = RiskReviewStrategyRequests.PlannedOf(account, command);
        if (planned.Count == 0)
            return ReportErrors.NoActiveSubPortfolios(account.Id);

        var holdings = await inputs.HoldingsAsync(account, cancellationToken);
        var labels = await inputs.LabelsAsync(cancellationToken);
        var snapshots = holdings.SubPortfolios.ToDictionary(sub => sub.SubPortfolioId);

        var heldIds = HeldInstruments(holdings, planned);
        var warnings = new List<string>();
        var prices = await inputs.PricesAsync(heldIds, asOf, account.BaseCurrency, warnings, cancellationToken);
        warnings.InsertRange(0, PlanningInputs.PriceWarnings(heldIds, prices, labels));

        if (request.Cause is { } cause)
            warnings.Insert(0, cause);

        var review = new RiskReviewInputs(labels, prices, warnings);
        var outcomes = Outcomes(planned, snapshots, assessments, review);

        var code = command.Scope?.Code ?? ReportDefaults.CodeOf(ReportKind.RiskReview);
        var title = command.Scope?.Title ?? ReportDefaults.TitleOf(ReportKind.RiskReview);
        var marketAsOf = prices.Count > 0 ? prices.Values.Max(price => price.Date) : asOf;
        var header = new ReportHeader(
            AccountId: account.Id,
            OwnerId: UserId.Of(account.OwnerId),
            AccountName: account.Name,
            Currency: Currency.Of(account.BaseCurrency),
            Period: YearMonth.Containing(asOf),
            Kind: ReportKind.RiskReview,
            Trigger: request.Trigger,
            MarketAsOf: marketAsOf,
            Code: code,
            Title: title);

        var draft = ReportComposer.Compose(header, null, outcomes, warnings);
        var report = RebalancingReport.Generate(draft, now);

        await SaveAsync(report, account.Id, code, asOf, now, request, cancellationToken);

        return report.Id;
    }

    private static List<Guid> HeldInstruments(AccountHoldingsDto holdings, List<SubPortfolioSetupDto> planned) =>
        [.. holdings.SubPortfolios
            .Where(sub => planned.Any(each => each.Id == sub.SubPortfolioId))
            .SelectMany(sub => sub.Positions.Select(position => position.InstrumentId))];

    private static List<SubPortfolioOutcome> Outcomes(
        List<SubPortfolioSetupDto> planned,
        Dictionary<Guid, SubPortfolioHoldingsDto> snapshots,
        IReadOnlyDictionary<Guid, StrategyEvaluationResult<RiskAssessmentDto>> assessments,
        RiskReviewInputs review)
    {
        var outcomes = new List<SubPortfolioOutcome>();

        foreach (var sub in planned)
        {
            var snapshot = snapshots.GetValueOrDefault(sub.Id);
            if (snapshot is null || snapshot.Positions.Count == 0)
                continue;

            var result = assessments.GetValueOrDefault(sub.Id) ?? new(null, "strategy.no_answer", "стратегія не відповіла");

            outcomes.Add(result.IsSuccess
                ? AssessedOutcome(sub, snapshot, result.Value!, review)
                : FailedOutcome(sub, result.ErrorMessage, review));
        }

        return outcomes;
    }

    private static SubPortfolioOutcome FailedOutcome(SubPortfolioSetupDto sub, string? error, RiskReviewInputs review)
    {
        review.Warnings.Add($"Оцінка ризику під-портфеля «{sub.Name}» не вдалася: {error}.");

        return new SubPortfolioOutcome(sub.Id, sub.Name, sub.TargetShare, 0m, null, null, null, [], [], Error: error);
    }

    private static SubPortfolioOutcome AssessedOutcome(
        SubPortfolioSetupDto sub, SubPortfolioHoldingsDto snapshot, RiskAssessmentDto assessment, RiskReviewInputs review)
    {
        var orders = new List<PlannedOrder>();
        var notes = new List<string>();

        foreach (var signal in assessment.Signals)
        {
            var position = snapshot.Positions.FirstOrDefault(each => each.InstrumentId == signal.InstrumentId);

            if (signal.Kind == ExitSignal && position is not null && review.Prices.TryGetValue(signal.InstrumentId, out var price))
                orders.Add(ExitOrder(signal.InstrumentId, position, price, signal.Reason));
            else
                notes.Add($"{review.LabelOf(signal.InstrumentId)}: {signal.Reason}");
        }

        return new SubPortfolioOutcome(
            SubPortfolioId: sub.Id,
            Name: sub.Name,
            TargetShare: sub.TargetShare,
            Contribution: 0m,
            Strategy: new StrategyInfo(assessment.StrategyId, assessment.StrategyVersion, assessment.StrategyCode, assessment.EngineVersion),
            Decision: null,
            Plan: null,
            Orders: orders,
            Notes: notes,
            Risk: SectionRiskOf(assessment));
    }

    private const string ExitSignal = "exit";

    private static PlannedOrder ExitOrder(Guid instrumentId, PositionDto position, LatestPriceDto price, string reason) => new(
        OrderKind.Exit,
        instrumentId,
        position.Quantity,
        price.Close,
        position.Quantity * price.Close,
        $"сигнал виходу: {reason}");

    private static SectionRisk SectionRiskOf(RiskAssessmentDto assessment) => new(
        assessment.Status,
        assessment.MarketAlerts,
        [.. assessment.Positions.Select(SectionRiskPositionOf)],
        assessment.Portfolio.Alerts,
        assessment.Actions);

    private static SectionRiskPosition SectionRiskPositionOf(PositionRiskDto position) => new(
        InstrumentId: position.InstrumentId,
        Status: position.Status,
        Quantity: position.Quantity,
        AverageCost: position.AverageCost,
        CurrentPrice: position.CurrentPrice,
        Value: position.Value,
        PnlPct: position.PnlPct,
        DrawdownFromHigh: position.DrawdownFromHigh,
        ExitPrice: position.ExitPrice,
        ExitRule: position.ExitRule,
        ExitDistance: position.ExitDistance,
        Alerts: position.Alerts,
        Sell: position.Sell);

    private async Task SaveAsync(
        RebalancingReport report, Guid accountId, string code, DateOnly asOf, DateTimeOffset now, ReportRequest request, CancellationToken cancellationToken)
    {
        await taxes.EstimateAsync(report, asOf, cancellationToken);
        reports.Add(report);

        var open = await reports.OpenAsync(accountId, cancellationToken);

        foreach (var superseded in open.Where(each => each.Kind == ReportKind.RiskReview && each.Code == code))
            superseded.SupersedeBy(report.Id, now);

        report.Generated(request.RequestedAt, now);
        request.Complete(report.Id, now);
        await reports.SaveChangesAsync(cancellationToken);
    }

    private sealed record RiskReviewInputs(
        IReadOnlyDictionary<Guid, string> Labels, IReadOnlyDictionary<Guid, LatestPriceDto> Prices, List<string> Warnings)
    {
        public string LabelOf(Guid instrumentId) => Labels.TryGetValue(instrumentId, out var label) ? label : instrumentId.ToString();
    }
}
