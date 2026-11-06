using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Backtesting.Replays.Infrastructure;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class StrategyAnswersTests
{
    private const string EngineRefusal = """@strategy.engine_rejected {"details":"no price"}""";
    private static readonly DateOnly Day = new(2025, 12, 31);

    [Fact]
    public async Task A_refused_risk_review_says_on_which_day_and_why_the_strategy_did_not_answer()
    {
        var answers = new StrategyAnswers(new RefusingEvaluation());

        var assessed = await answers.AssessAsync(Setup(), new ReplayBook(1000m), Day, [], CancellationToken.None);

        assessed.Error.AsMessage().ShouldBe(Message.Of("backtest.strategy_failed", new { day = Day, reason = EngineRefusal }));
    }

    [Fact]
    public async Task A_refused_rebalance_says_on_which_day_and_why_the_strategy_did_not_answer()
    {
        var answers = new StrategyAnswers(new RefusingEvaluation());

        var decided = await answers.DecideAsync(Setup(), new ReplayBook(1000m), Day, [], CancellationToken.None);

        decided.Error.AsMessage().ShouldBe(Message.Of("backtest.strategy_failed", new { day = Day, reason = EngineRefusal }));
    }

    private static ReplaySetup Setup() => new(
        StrategyId: Guid.NewGuid(),
        OwnerId: UserId.New(),
        Version: 1,
        CanBeReplayed: true,
        Universe: [Guid.NewGuid()],
        CashInstrumentId: null,
        Benchmark: new ReplayBenchmark(Guid.NewGuid(), "VGWL"),
        RebalanceCadence: ReplayCadence.Monthly,
        RiskReviewCadence: ReplayCadence.Weekly,
        Signals: ReplaySignalTriggers.None,
        Currency: "EUR",
        Comparisons: []);

    private sealed class RefusingEvaluation : IStrategyEvaluation
    {
        public Task<StrategyEvaluationResult<StrategyDecisionDto>> EvaluateAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new StrategyEvaluationResult<StrategyDecisionDto>(null, "strategy.engine_rejected", EngineRefusal));

        public Task<StrategyEvaluationResult<RiskAssessmentDto>> AssessRiskAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new StrategyEvaluationResult<RiskAssessmentDto>(null, "strategy.engine_rejected", EngineRefusal));

        public Task<StrategyEvaluationResult<StrategyJobTicket>> SubmitAsync(StrategyTask task, EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<StrategyJobPoll<StrategyDecisionDto>> PollDecisionAsync(StrategyJobTicket ticket, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<StrategyJobPoll<RiskAssessmentDto>> PollRiskAsync(StrategyJobTicket ticket, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
