namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record StrategyTargets(
    bool MarketIsFavorable,
    Percentage InvestFraction,
    IReadOnlyList<TargetWeight> Targets,
    IReadOnlyList<InstrumentScore> Scores)
{
    private const int PercentDigits = 2;

    public static StrategyTargets Of(bool marketIsFavorable, decimal investFraction, IReadOnlyList<TargetWeight> targets, IReadOnlyList<InstrumentScore> scores) => new(
        MarketIsFavorable: marketIsFavorable,
        InvestFraction: Percentage.Of(Math.Clamp(investFraction, 0m, 1m)),
        Targets: targets,
        Scores: scores);

    public decimal TotalWeight => Targets.Sum(target => target.Weight);

    public bool Selects(Guid instrumentId) => Targets.Any(target => target.InstrumentId == instrumentId);

    public InstrumentScore? ScoreOf(Guid instrumentId) => Scores.FirstOrDefault(score => score.InstrumentId == instrumentId);

    public bool CoversLessThanWhole => decimal.Round(TotalWeight, PercentDigits) < 1m;

    public StrategyTargets WithTargets(IReadOnlyList<TargetWeight> targets) => this with { Targets = targets };
}
