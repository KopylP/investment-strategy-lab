using System.Text.Json;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class StrategyRuns(IStrategyEvaluation strategies) : IStrategyRuns
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<StrategySubmission> SubmitAsync(ReportKind kind, StrategyAsk ask, CancellationToken cancellationToken)
    {
        var task = kind == ReportKind.RiskReview ? StrategyTask.AssessRisk : StrategyTask.Decide;

        var submitted = await strategies.SubmitAsync(task, RequestOf(ask), cancellationToken);
        if (submitted.IsSuccess)
            return StrategySubmission.Accepted(JobOf(submitted.Value!));

        var code = submitted.ErrorCode!;

        return StrategySubmission.Refused(code, submitted.ErrorMessage ?? code, code == StrategyErrorCodes.EngineUnavailable);
    }

    public async Task<StrategyPoll> PollAsync(StrategyJob job, CancellationToken cancellationToken)
    {
        var ticket = new StrategyJobTicket(
            JobId: job.JobId,
            Task: Enum.Parse<StrategyTask>(job.Task),
            StrategyId: job.StrategyId,
            StrategyVersion: job.StrategyVersion,
            CashInstrumentId: job.CashInstrumentId);

        return ticket.Task == StrategyTask.AssessRisk
            ? PollOf(await strategies.PollRiskAsync(ticket, cancellationToken))
            : PollOf(await strategies.PollDecisionAsync(ticket, cancellationToken));
    }

    public StrategyDecision DecisionIn(string answer)
    {
        var decision = JsonSerializer.Deserialize<StrategyDecisionDto>(answer, Json)!;

        return new StrategyDecision(
            Strategy: StrategyInfoOf(decision.StrategyId, decision.StrategyVersion, decision.StrategyCode, decision.EngineVersion),
            Targets: TargetsOf(decision),
            Section: SectionOf(decision),
            CashInstrumentId: decision.CashInstrumentId);
    }

    public StrategyRiskAssessment RiskIn(string answer)
    {
        var assessment = JsonSerializer.Deserialize<RiskAssessmentDto>(answer, Json)!;

        return new StrategyRiskAssessment(
            Strategy: StrategyInfoOf(assessment.StrategyId, assessment.StrategyVersion, assessment.StrategyCode, assessment.EngineVersion),
            Signals: [.. assessment.Signals.Select(signal => new RiskSignal(signal.InstrumentId, signal.Kind, signal.Reason))],
            Section: SectionRiskOf(assessment));
    }

    private static StrategyInfo StrategyInfoOf(Guid strategyId, int version, string code, string engineVersion) => new(
        StrategyId: strategyId,
        Version: version,
        Code: code,
        EngineVersion: engineVersion);

    private static EvaluateStrategyRequest RequestOf(StrategyAsk ask)
    {
        var state = new SubPortfolioStateDto(ask.Currency, ask.Cash, [.. ask.Positions.Select(HoldingStateOf)]);

        return new EvaluateStrategyRequest(
            StrategyId: ask.StrategyId,
            Version: ask.Version,
            SubPortfolio: state,
            Contribution: ask.Contribution,
            AsOf: ask.AsOf,
            CashInstrumentId: ask.CashInstrumentId);
    }

    private static HoldingStateDto HoldingStateOf(IHeldPosition position)
    {
        var lots = position.Lots.Select(lot => new LotStateDto(lot.Quantity, lot.UnitCost, lot.OpenedOn));

        return new HoldingStateDto(position.InstrumentId, [.. lots]);
    }

    private static StrategyJob JobOf(StrategyJobTicket ticket) => new(
        JobId: ticket.JobId,
        Task: ticket.Task.ToString(),
        StrategyId: ticket.StrategyId,
        StrategyVersion: ticket.StrategyVersion,
        CashInstrumentId: ticket.CashInstrumentId);

    private static StrategyPoll PollOf<T>(StrategyJobPoll<T> poll) where T : class
    {
        var state = StateOf(poll.State);

        return poll.Result switch
        {
            { IsSuccess: true, Value: { } value } => new StrategyPoll(state, JsonSerializer.Serialize(value, Json), null, null),
            { } failed => new StrategyPoll(state, null, failed.ErrorCode, failed.ErrorMessage),
            null => new StrategyPoll(state, null, null, null),
        };
    }

    private static StrategyRunState StateOf(StrategyJobState state) => state switch
    {
        StrategyJobState.Running => StrategyRunState.Running,
        StrategyJobState.Finished => StrategyRunState.Finished,
        StrategyJobState.Lost => StrategyRunState.Lost,
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown state of a strategy job."),
    };

    private static StrategyTargets TargetsOf(StrategyDecisionDto decision)
    {
        var targets = decision.Targets.Select(target => new TargetWeight(target.InstrumentId, target.Weight));
        var scores = decision.Rationale.Scores.Select(score => new InstrumentScore(score.InstrumentId, score.Rank, score.Momentum, score.Reason));

        return StrategyTargets.Of(decision.Market.IsFavorable, decision.InvestFraction, [.. targets], [.. scores]);
    }

    private static SectionDecision SectionOf(StrategyDecisionDto decision) => new(
        MarketIsFavorable: decision.Market.IsFavorable,
        InvestFraction: decision.InvestFraction,
        MarketReasons: decision.Market.Reasons,
        Summary: decision.Rationale.Summary,
        Targets: [.. decision.Targets.Select(target => new SectionTarget(target.InstrumentId, target.Weight))],
        Scores: [.. decision.Rationale.Scores.Select(SectionScoreOf)],
        Warnings: decision.Warnings,
        ScoreTitle: decision.Rationale.Title,
        InstrumentLabel: decision.Rationale.InstrumentLabel,
        Columns: decision.Rationale.Columns?.Select(SectionColumnOf).ToList());

    private static SectionScore SectionScoreOf(InstrumentScoreDto score) => new(
        InstrumentId: score.InstrumentId,
        Name: score.Name,
        Group: score.Group,
        Price: score.Price,
        Momentum: score.Momentum,
        Return1m: score.Return1m,
        Return3m: score.Return3m,
        Return12m: score.Return12m,
        Volatility: score.Volatility,
        AboveMovingAverage: score.AboveMovingAverage,
        Rank: score.Rank,
        Status: score.Status,
        Reason: score.Reason,
        Values: score.Values);

    private static SectionColumn SectionColumnOf(RationaleColumnDto column) => new(
        Id: column.Id,
        Label: column.Label,
        Format: column.Format,
        Hint: column.Hint,
        Compact: column.Compact,
        TrueHint: column.TrueHint,
        FalseHint: column.FalseHint);

    private static SectionRisk SectionRiskOf(RiskAssessmentDto assessment) => new(
        Status: assessment.Status,
        MarketAlerts: assessment.MarketAlerts,
        Positions: [.. assessment.Positions.Select(SectionRiskPositionOf)],
        PortfolioAlerts: assessment.Portfolio.Alerts,
        Actions: assessment.Actions);

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
}
