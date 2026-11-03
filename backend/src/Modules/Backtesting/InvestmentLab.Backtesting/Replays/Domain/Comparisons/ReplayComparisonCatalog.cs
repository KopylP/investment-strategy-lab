namespace InvestmentLab.Backtesting.Replays.Domain.Comparisons;

public sealed class ReplayComparisonCatalog(IEnumerable<IReplayComparison> comparisons)
{
    private readonly Dictionary<string, IReplayComparison> _byCode = comparisons.ToDictionary(comparison => comparison.Code);

    public Result<IReadOnlyList<IReplayComparison>> Resolve(IReadOnlyList<string> codes)
    {
        if (codes.FirstOrDefault(code => !_byCode.ContainsKey(code)) is { } unknown)
            return BacktestErrors.UnknownComparison(unknown);

        IReadOnlyList<IReplayComparison> resolved = [.. codes.Select(code => _byCode[code])];

        return Result<IReadOnlyList<IReplayComparison>>.Success(resolved);
    }
}
