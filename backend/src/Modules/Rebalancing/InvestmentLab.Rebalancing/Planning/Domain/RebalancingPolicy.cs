namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record RebalancingPolicy
{
    public static readonly decimal DefaultMinTrade = 25m;
    public static readonly Percentage DefaultTrimBand = Percentage.FromPercent(10m);

    public decimal MinTrade { get; }
    public Percentage TrimBand { get; }
    public bool AllowFractionalShares { get; }

    private RebalancingPolicy(decimal minTrade, Percentage trimBand, bool allowFractionalShares) =>
        (MinTrade, TrimBand, AllowFractionalShares) = (minTrade, trimBand, allowFractionalShares);

    public static RebalancingPolicy Default => new(DefaultMinTrade, DefaultTrimBand, true);

    public static RebalancingPolicy Of(decimal minTrade, Percentage trimBand, bool allowFractionalShares) =>
        minTrade >= 0m
            ? new(minTrade, trimBand, allowFractionalShares)
            : throw new ArgumentOutOfRangeException(nameof(minTrade), minTrade, "Minimum trade cannot be negative.");

    public static Result<RebalancingPolicy> TryOf(decimal minTrade, decimal trimBand, bool allowFractionalShares)
    {
        if (minTrade < 0m)
            return Error.Validation("rebalancing.min_trade_negative", $"Minimum trade {minTrade} cannot be negative.");

        if (Percentage.TryOf(trimBand) is not { IsSuccess: true } band)
            return Error.Validation("rebalancing.trim_band_out_of_range", $"Trim band {trimBand} must be between 0 and 1.");

        return new RebalancingPolicy(minTrade, band.Value, allowFractionalShares);
    }
}
