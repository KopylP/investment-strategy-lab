using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;

public sealed class GenerateRiskReviewHandler(PlanningInputs inputs, IReportRepository reports, GeneratedReportSaver saver, TimeProvider clock)
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
        var review = await ReviewInputsAsync(account, holdings, planned, asOf, request, cancellationToken);
        var snapshots = holdings.SubPortfolios.ToDictionary(sub => sub.SubPortfolioId);
        var outcomes = Outcomes(planned, snapshots, assessments, review);

        var header = HeaderOf(account, command, asOf, request, review);
        var draft = ReportComposer.Compose(header, null, outcomes, review.Warnings);
        var report = RebalancingReport.Generate(draft, now);

        var open = await reports.OpenAsync(account.Id, cancellationToken);
        await saver.SaveAsync(report, open, asOf, now, request, cancellationToken);

        return report.Id;
    }

    private async Task<RiskReviewInputs> ReviewInputsAsync(
        AccountSetupDto account,
        AccountHoldingsDto holdings,
        List<SubPortfolioSetupDto> planned,
        DateOnly asOf,
        ReportRequest request,
        CancellationToken cancellationToken)
    {
        var labels = await inputs.LabelsAsync(cancellationToken);
        var heldIds = HeldInstruments(holdings, planned);

        var warnings = new List<string>();
        var prices = await inputs.PricesAsync(heldIds, asOf, account.BaseCurrency, warnings, cancellationToken);
        warnings.InsertRange(0, PlanningInputs.PriceWarnings(heldIds, prices, labels));

        if (request.Cause is { } cause)
            warnings.Insert(0, cause);

        return new RiskReviewInputs(labels, prices, warnings);
    }

    private static ReportHeader HeaderOf(
        AccountSetupDto account, GenerateRiskReviewCommand command, DateOnly asOf, ReportRequest request, RiskReviewInputs review) => new(
        AccountId: account.Id,
        OwnerId: UserId.Of(account.OwnerId),
        AccountName: account.Name,
        Currency: Currency.Of(account.BaseCurrency),
        Period: YearMonth.Containing(asOf),
        Kind: ReportKind.RiskReview,
        Trigger: request.Trigger,
        MarketAsOf: ReportHeader.MarketDateOf([.. review.Prices.Values.Select(price => price.Date)], asOf),
        Code: command.Scope?.Code ?? ReportDefaults.CodeOf(ReportKind.RiskReview),
        Title: command.Scope?.Title ?? ReportDefaults.TitleOf(ReportKind.RiskReview));

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

        return SubPortfolioOutcome.Failed(sub.Id, sub.Name, sub.TargetShare, 0m, error);
    }

    private static SubPortfolioOutcome AssessedOutcome(
        SubPortfolioSetupDto sub, SubPortfolioHoldingsDto snapshot, RiskAssessmentDto assessment, RiskReviewInputs review)
    {
        var signals = assessment.Signals.Select(signal => new RiskSignal(signal.InstrumentId, signal.Kind, signal.Reason));
        var exits = ExitPlan.Of(signals, HeldQuantities(snapshot), Closes(review), review.LabelOf);

        return new SubPortfolioOutcome(
            SubPortfolioId: sub.Id,
            Name: sub.Name,
            TargetShare: sub.TargetShare,
            Contribution: 0m,
            Strategy: new StrategyInfo(assessment.StrategyId, assessment.StrategyVersion, assessment.StrategyCode, assessment.EngineVersion),
            Decision: null,
            Plan: null,
            Orders: exits.Orders,
            Notes: exits.Notes,
            Risk: SectionRiskOf(assessment));
    }

    private static Dictionary<Guid, decimal> HeldQuantities(SubPortfolioHoldingsDto snapshot)
    {
        var quantities = new Dictionary<Guid, decimal>();

        foreach (var position in snapshot.Positions)
            quantities.TryAdd(position.InstrumentId, position.Quantity);

        return quantities;
    }

    private static Dictionary<Guid, decimal> Closes(RiskReviewInputs review) =>
        review.Prices.ToDictionary(price => price.Key, price => price.Value.Close);

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

    private sealed record RiskReviewInputs(
        IReadOnlyDictionary<Guid, string> Labels, IReadOnlyDictionary<Guid, LatestPriceDto> Prices, List<string> Warnings)
    {
        public string LabelOf(Guid instrumentId) => Labels.TryGetValue(instrumentId, out var label) ? label : instrumentId.ToString();
    }
}
