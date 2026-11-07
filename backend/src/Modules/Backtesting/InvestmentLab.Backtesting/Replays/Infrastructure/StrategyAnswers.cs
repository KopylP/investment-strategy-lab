using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Strategies.Contracts;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class StrategyAnswers(IStrategyEvaluation evaluation, TimeProvider clock, ILogger<StrategyAnswers> logger) : IStrategyAnswers
{
    private const string ExitSignal = "exit";

    private readonly Dictionary<ReplayWindow, string?> _priceSets = [];

    public async Task<Result<ReplayDecision>> DecideAsync(ReplaySetup setup, ReplayBook book, DateOnly day, IReadOnlyList<ReplaySignalState> states, CancellationToken cancellationToken)
    {
        var request = RequestOf(setup, book, day, states);
        var answer = await AskAsync(setup, request, evaluation.EvaluateAsync, cancellationToken);
        if (!answer.IsSuccess)
            return FailureOf(day, answer.ErrorMessage);

        var decided = answer.Value!;
        var targets = decided.Targets.Select(target => new ReplayTarget(target.InstrumentId, target.Weight)).ToList();
        var explanation = DecisionExplanation(decided);

        return new ReplayDecision(decided.Market.IsFavorable, decided.InvestFraction, targets, explanation);
    }

    public async Task<Result<ReplayRiskDecision>> AssessAsync(ReplaySetup setup, ReplayBook book, DateOnly day, IReadOnlyList<ReplaySignalState> states, CancellationToken cancellationToken)
    {
        var request = RequestOf(setup, book, day, states);
        var answer = await AskAsync(setup, request, evaluation.AssessRiskAsync, cancellationToken);
        if (!answer.IsSuccess)
            return FailureOf(day, answer.ErrorMessage);

        var assessment = answer.Value!;
        var exits = ExitsOf(assessment);
        var explanation = RiskExplanation(assessment);

        return new ReplayRiskDecision(exits, explanation);
    }

    private async Task<StrategyEvaluationResult<T>> AskAsync<T>(
        ReplaySetup setup,
        EvaluateStrategyRequest request,
        Func<EvaluateStrategyRequest, CancellationToken, Task<StrategyEvaluationResult<T>>> ask,
        CancellationToken cancellationToken) where T : class
    {
        var window = ReplayWindow.Of(setup);
        if (!_priceSets.TryGetValue(window, out var priceSet))
            priceSet = await UploadAsync(setup, window, request.AsOf, cancellationToken);

        var answer = await ask(request with { PriceSetId = priceSet }, cancellationToken);
        if (answer.ErrorCode != StrategyErrorCodes.PriceSetUnknown)
            return answer;

        var uploadedAgain = await UploadAsync(setup, window, request.AsOf, cancellationToken);

        return await ask(request with { PriceSetId = uploadedAgain }, cancellationToken);
    }

    private async Task<string?> UploadAsync(ReplaySetup setup, ReplayWindow window, DateOnly firstDay, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var upload = new UploadPriceSetRequest(
            StrategyId: setup.StrategyId,
            Version: setup.Version,
            FirstAsOf: firstDay,
            LastAsOf: setup.Experiment?.End ?? today,
            ValuationCurrency: setup.Currency,
            ParametersOverride: setup.ParametersOverride);

        var uploaded = await evaluation.UploadPriceSetAsync(upload, cancellationToken);
        if (!uploaded.IsSuccess)
            logger.LogWarning("Prices of strategy {Strategy} could not be uploaded ({Error}); its replay sends them with every call", setup.StrategyId, uploaded.ErrorMessage);

        var priceSet = uploaded.Value?.Id;
        _priceSets[window] = priceSet;

        return priceSet;
    }

    private static ReplayExplanation DecisionExplanation(StrategyDecisionDto decided)
    {
        var rationale = decided.Rationale;
        var candidates = rationale.Scores.Select(CandidateOf).ToList();
        var valueLabels = rationale.Columns?.ToDictionary(column => column.Id, column => column.Label);

        return new ReplayExplanation(
            Summary: rationale.Summary,
            EngineVersion: decided.EngineVersion,
            Candidates: candidates,
            ValueLabels: valueLabels);
    }

    private static ReplayCandidate CandidateOf(InstrumentScoreDto score) => new(
        InstrumentId: score.InstrumentId,
        Name: score.Name,
        Status: score.Status,
        Reason: score.Reason,
        Weight: score.Weight,
        Values: score.Values);

    private static List<Guid> ExitsOf(RiskAssessmentDto assessment)
    {
        var exitSignals = assessment.Signals.Where(signal => signal.Kind == ExitSignal);

        return [.. exitSignals.Select(signal => signal.InstrumentId)];
    }

    private static ReplayExplanation RiskExplanation(RiskAssessmentDto assessment)
    {
        var summary = Message.Of("research.risk.actions", new { actions = assessment.Actions });
        var candidates = assessment.Signals.Select(SignalCandidateOf).ToList();

        return new ReplayExplanation(
            Summary: summary,
            EngineVersion: assessment.EngineVersion,
            Candidates: candidates);
    }

    private static ReplayCandidate SignalCandidateOf(RiskSignalDto signal) => new(
        InstrumentId: signal.InstrumentId,
        Name: string.Empty,
        Status: signal.Kind,
        Reason: signal.Reason,
        Weight: null);

    private static EvaluateStrategyRequest RequestOf(ReplaySetup setup, ReplayBook book, DateOnly day, IReadOnlyList<ReplaySignalState> states)
    {
        var holdings = book.Positions.Select(position => new HoldingStateDto(
            position.InstrumentId,
            [.. position.Lots.Select(lot => new LotStateDto(lot.Quantity, lot.UnitCost, lot.AcquiredOn))]));
        var state = new SubPortfolioStateDto(setup.Currency, Math.Max(0m, book.Cash), [.. holdings]);

        var signalStates = states.Select(signal => new SignalStateDto(signal.SignalId, signal.ActiveFor)).ToList();

        return new EvaluateStrategyRequest(
            StrategyId: setup.StrategyId,
            Version: setup.Version,
            SubPortfolio: state,
            Contribution: 0m,
            AsOf: day,
            SignalStates: signalStates,
            ValuationCurrency: setup.Currency,
            ParametersOverride: setup.ParametersOverride);
    }

    private static Error FailureOf(DateOnly day, string? reason) =>
        Error.Validation("backtest.strategy_failed", $"The strategy did not answer on {day:yyyy-MM-dd}: {reason}", new { day, reason });
}
