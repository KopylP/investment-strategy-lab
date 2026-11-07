using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Experiments;

public sealed class ResearchPlanTests
{
    private static readonly ExperimentPeriod Period = new(new DateOnly(2017, 1, 2), new DateOnly(2026, 10, 30));
    private static readonly ExperimentPeriod LaterTraining = new(new DateOnly(2019, 1, 2), new DateOnly(2022, 12, 30));
    private static readonly ExperimentPeriod TrainingFromStart = Period with { End = new DateOnly(2022, 12, 30) };

    [Fact]
    public void A_single_candidate_needs_no_training_replay()
    {
        var plan = PlanOf(new ParameterVariant("{}", null));

        plan.TrainingReadFromFullRuns(LaterTraining).ShouldBeTrue();
    }

    [Fact]
    public void Training_from_the_start_of_the_period_is_read_from_the_full_runs_of_every_candidate()
    {
        var plan = PlanOf(new ParameterVariant("{}", 1m), new ParameterVariant("{\"a\":2}", 2m));

        plan.TrainingReadFromFullRuns(TrainingFromStart).ShouldBeTrue();
    }

    [Fact]
    public void A_later_training_window_with_several_candidates_is_replayed()
    {
        var plan = PlanOf(new ParameterVariant("{}", 1m), new ParameterVariant("{\"a\":2}", 2m));

        plan.TrainingReadFromFullRuns(LaterTraining).ShouldBeFalse();
    }

    private static ResearchPlan PlanOf(params ParameterVariant[] variants)
    {
        var setup = new ReplaySetup(
            StrategyId: Guid.NewGuid(),
            OwnerId: UserId.New(),
            Version: 1,
            CanBeReplayed: true,
            Universe: [Guid.NewGuid()],
            CashInstrumentId: null,
            Benchmark: new ReplayBenchmark(Guid.NewGuid(), "Benchmark"),
            RebalanceCadence: ReplayCadence.Monthly,
            RiskReviewCadence: null,
            Signals: ReplaySignalTriggers.None,
            Currency: "EUR",
            Comparisons: []);
        var source = new ResearchSource(setup, "{}", "rule-based");
        var conditions = new ExperimentConditions(Period, 10_000m, new TradingCosts(), new RobustnessPlan());

        return new ResearchPlan(source, conditions, variants, [260]);
    }
}
