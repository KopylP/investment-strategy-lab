using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Signals.Contracts;

namespace InvestmentLab.Performance.Valuation.Infrastructure;

internal sealed class PortfolioReturnHistory(IPerformanceRepository store, SnapshotBuilder builder) : IPortfolioReturnHistory
{
    public async Task<IReadOnlyList<TwrPointDto>> GetTwrHistory(
        Guid accountId,
        Guid? subPortfolioId,
        DateOnly from,
        CancellationToken cancellationToken)
    {
        var refreshed = await builder.EnsureFreshAsync(accountId, cancellationToken);
        if (refreshed.IsFailure)
            return [];

        var snapshots = await store.SnapshotsAsync(accountId, SnapshotScenario.Actual, from, cancellationToken);

        return [.. snapshots
            .Where(snapshot => snapshot.SubPortfolioId == subPortfolioId)
            .Select(snapshot => new TwrPointDto(snapshot.Date, snapshot.TwrIndex))];
    }
}
