using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Backtesting.Replays.Infrastructure;
using InvestmentLab.Strategies.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvestmentLab.Backtesting.Tests.Replays;

public sealed class StrategyAnswersTests
{
    private const string EngineRefusal = """@strategy.engine_rejected {"details":"no price"}""";
    private static readonly DateOnly Day = new(2025, 12, 31);

    [Fact]
    public async Task A_refused_risk_review_says_on_which_day_and_why_the_strategy_did_not_answer()
    {
        var answers = new StrategyAnswers(new RefusingEvaluation(), TimeProvider.System, NullLogger<StrategyAnswers>.Instance);

        var assessed = await answers.AssessAsync(Setup(), new ReplayBook(1000m), Day, [], CancellationToken.None);

        assessed.Error.AsMessage().ShouldBe(Message.Of("backtest.strategy_failed", new { day = Day, reason = EngineRefusal }));
    }

    [Fact]
    public async Task A_refused_rebalance_says_on_which_day_and_why_the_strategy_did_not_answer()
    {
        var answers = new StrategyAnswers(new RefusingEvaluation(), TimeProvider.System, NullLogger<StrategyAnswers>.Instance);

        var decided = await answers.DecideAsync(Setup(), new ReplayBook(1000m), Day, [], CancellationToken.None);

        decided.Error.AsMessage().ShouldBe(Message.Of("backtest.strategy_failed", new { day = Day, reason = EngineRefusal }));
    }

    [Fact]
    public async Task A_replay_uploads_its_prices_once_and_names_them_in_every_call()
    {
        var evaluation = new PricedEvaluation();
        var answers = new StrategyAnswers(evaluation, TimeProvider.System, NullLogger<StrategyAnswers>.Instance);
        var setup = Setup();

        await answers.DecideAsync(setup, new ReplayBook(1000m), Day, [], CancellationToken.None);
        await answers.AssessAsync(setup, new ReplayBook(1000m), Day.AddDays(7), [], CancellationToken.None);
        await answers.DecideAsync(setup, new ReplayBook(1000m), Day.AddDays(30), [], CancellationToken.None);

        evaluation.Uploads.ShouldHaveSingleItem().FirstAsOf.ShouldBe(Day);
        evaluation.NamedSets.ShouldBe(["set-1", "set-1", "set-1"]);
    }

    [Fact]
    public async Task A_price_set_the_engine_forgot_is_uploaded_again_and_the_call_asked_once_more()
    {
        var evaluation = new PricedEvaluation { ForgetsSet = "set-1" };
        var answers = new StrategyAnswers(evaluation, TimeProvider.System, NullLogger<StrategyAnswers>.Instance);

        var decided = await answers.DecideAsync(Setup(), new ReplayBook(1000m), Day, [], CancellationToken.None);

        decided.IsSuccess.ShouldBeTrue();
        evaluation.Uploads.Count.ShouldBe(2);
        evaluation.NamedSets.ShouldBe(["set-1", "set-2"]);
    }

    [Fact]
    public async Task A_replay_whose_prices_cannot_be_uploaded_sends_them_with_every_call()
    {
        var evaluation = new PricedEvaluation { UploadFails = true };
        var answers = new StrategyAnswers(evaluation, TimeProvider.System, NullLogger<StrategyAnswers>.Instance);

        var decided = await answers.DecideAsync(Setup(), new ReplayBook(1000m), Day, [], CancellationToken.None);

        decided.IsSuccess.ShouldBeTrue();
        evaluation.NamedSets.ShouldBe([null]);
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

    private sealed class PricedEvaluation : IStrategyEvaluation
    {
        public List<UploadPriceSetRequest> Uploads { get; } = [];
        public List<string?> NamedSets { get; } = [];
        public string? ForgetsSet { get; init; }
        public bool UploadFails { get; init; }

        public Task<StrategyEvaluationResult<PriceSetTicket>> UploadPriceSetAsync(UploadPriceSetRequest request, CancellationToken cancellationToken)
        {
            Uploads.Add(request);
            var ticket = UploadFails
                ? new StrategyEvaluationResult<PriceSetTicket>(null, StrategyErrorCodes.EngineUnavailable, "down")
                : new StrategyEvaluationResult<PriceSetTicket>(new PriceSetTicket($"set-{Uploads.Count}"));

            return Task.FromResult(ticket);
        }

        public Task<StrategyEvaluationResult<StrategyDecisionDto>> EvaluateAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Answer(request, Decision(request)));

        public Task<StrategyEvaluationResult<RiskAssessmentDto>> AssessRiskAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Answer(request, Assessment(request)));

        public Task<StrategyEvaluationResult<StrategyJobTicket>> SubmitAsync(StrategyTask task, EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<StrategyJobPoll<StrategyDecisionDto>> PollDecisionAsync(StrategyJobTicket ticket, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<StrategyJobPoll<RiskAssessmentDto>> PollRiskAsync(StrategyJobTicket ticket, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        private StrategyEvaluationResult<T> Answer<T>(EvaluateStrategyRequest request, T answer) where T : class
        {
            NamedSets.Add(request.PriceSetId);
            if (request.PriceSetId is not null && request.PriceSetId == ForgetsSet)
                return new StrategyEvaluationResult<T>(null, StrategyErrorCodes.PriceSetUnknown, "forgotten");

            return new StrategyEvaluationResult<T>(answer);
        }

        private static StrategyDecisionDto Decision(EvaluateStrategyRequest request) => new(
            StrategyId: request.StrategyId,
            StrategyVersion: 1,
            StrategyCode: "rule-based",
            EngineVersion: "1",
            AsOf: request.AsOf,
            InvestFraction: 1m,
            Targets: [],
            Market: new MarketRegimeDto(true, null, null, null, []),
            Rationale: new RationaleDto("summary", null, []),
            Warnings: []);

        private static RiskAssessmentDto Assessment(EvaluateStrategyRequest request) => new(
            StrategyId: request.StrategyId,
            StrategyVersion: 1,
            StrategyCode: "rule-based",
            EngineVersion: "1",
            AsOf: request.AsOf,
            Status: "ok",
            MarketAlerts: [],
            Positions: [],
            Portfolio: new PortfolioRiskDto(0m, 0m, null, null, null, false, []),
            Signals: [],
            Actions: []);
    }

    private sealed class RefusingEvaluation : IStrategyEvaluation
    {
        public Task<StrategyEvaluationResult<PriceSetTicket>> UploadPriceSetAsync(UploadPriceSetRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new StrategyEvaluationResult<PriceSetTicket>(new PriceSetTicket("set")));

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
