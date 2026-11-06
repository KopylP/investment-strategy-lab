namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record TradingCosts(
    decimal CommissionRate = 0.001m,
    decimal FeePerTrade = 1m,
    decimal SpreadBps = 0m,
    decimal SlippageBps = 0m)
{
    private const decimal BasisPointsPerUnit = 10_000m;
    private const decimal FullSpreadSides = 2m;
    private const decimal MaximumCommissionRate = 0.1m;
    private const decimal MaximumFixedFeeShare = 0.1m;
    private const decimal MaximumAdverseBasisPoints = 500m;

    public decimal Friction => SpreadBps / (BasisPointsPerUnit * FullSpreadSides) + SlippageBps / BasisPointsPerUnit;

    public decimal FeeOf(decimal amount) => amount * CommissionRate + FeePerTrade;

    public decimal FillPrice(decimal quote, bool isSale)
    {
        var direction = isSale ? -1m : 1m;

        return quote * (1m + direction * Friction);
    }

    public decimal SpreadCostOf(decimal quantity, decimal quote) =>
        quantity * quote * SpreadBps / (BasisPointsPerUnit * FullSpreadSides);

    public decimal SlippageCostOf(decimal quantity, decimal quote) =>
        quantity * quote * SlippageBps / BasisPointsPerUnit;

    public Error? Problem(decimal capital)
    {
        if (CommissionRate is < 0m or > MaximumCommissionRate)
            return BacktestErrors.InvalidCosts();

        if (FeePerTrade < 0m || FeePerTrade > capital * MaximumFixedFeeShare)
            return BacktestErrors.InvalidCosts();

        if (IsInvalidFriction(SpreadBps) || IsInvalidFriction(SlippageBps))
            return BacktestErrors.InvalidCosts();

        return null;
    }

    private static bool IsInvalidFriction(decimal basisPoints) => basisPoints is < 0m or > MaximumAdverseBasisPoints;
}
