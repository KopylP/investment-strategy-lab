namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record StrategyTargets(
    bool MarketIsFavorable,
    Percentage InvestFraction,
    IReadOnlyList<TargetWeight> Targets,
    IReadOnlyList<InstrumentScore> Scores)
{
    public decimal TotalWeight => Targets.Sum(target => target.Weight);

    public bool Selects(Guid instrumentId) => Targets.Any(target => target.InstrumentId == instrumentId);

    public InstrumentScore? ScoreOf(Guid instrumentId) => Scores.FirstOrDefault(score => score.InstrumentId == instrumentId);

    public int? Slots =>
        Targets is [{ Weight: > 0m } first, ..] && Targets.All(target => target.Weight == first.Weight)
            ? (int)decimal.Round(1m / first.Weight)
            : null;

    public StrategyTargets WithTargets(IReadOnlyList<TargetWeight> targets) => this with { Targets = targets };
}
