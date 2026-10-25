namespace InvestmentLab.Rebalancing.Planning.Domain;

/// <summary>Share of the sub-portfolio a strategy wants in one instrument.</summary>
public sealed record TargetWeight(Guid InstrumentId, decimal Weight);

/// <summary>Why the strategy ranked an instrument the way it did; feeds the reason of each order.</summary>
public sealed record InstrumentScore(Guid InstrumentId, int? Rank, decimal? Momentum, string Reason);

/// <summary>
/// What the strategy decided for a sub-portfolio: targets in rank order, how much of new money to invest, and whether the
/// market is favorable (only then does parked cash go back to work).
/// </summary>
public sealed record StrategyTargets(
    bool MarketIsFavorable,
    Percentage InvestFraction,
    IReadOnlyList<TargetWeight> Targets,
    IReadOnlyList<InstrumentScore> Scores)
{
    public decimal TotalWeight => Targets.Sum(target => target.Weight);

    public bool Selects(Guid instrumentId) => Targets.Any(target => target.InstrumentId == instrumentId);

    public InstrumentScore? ScoreOf(Guid instrumentId) => Scores.FirstOrDefault(score => score.InstrumentId == instrumentId);

    /// <summary>Slots of an equal-weight strategy (1 / weight of a pick); null when weights differ or nothing is picked.</summary>
    public int? Slots =>
        Targets is [{ Weight: > 0m } first, ..] && Targets.All(target => target.Weight == first.Weight)
            ? (int)decimal.Round(1m / first.Weight)
            : null;

    public StrategyTargets WithTargets(IReadOnlyList<TargetWeight> targets) => this with { Targets = targets };
}

/// <summary>What a sub-portfolio holds of one instrument; <see cref="AverageCost"/> per unit, fees included.</summary>
public sealed record SubPortfolioPosition(Guid InstrumentId, Quantity Quantity, decimal AverageCost);
