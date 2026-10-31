using InvestmentLab.Performance.Contracts;
using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Valuation.Infrastructure;

internal sealed class ValuationHistory(IPerformanceRepository store, SnapshotBuilder builder) : IValuationHistory
{
    public async Task<IReadOnlyList<TwrPointDto>> GetTwrHistory(Guid accountId, Guid? subPortfolioId, DateOnly from, CancellationToken cancellationToken)
    {
        await builder.EnsureFreshAsync(accountId, cancellationToken);

        var snapshots = await store.SnapshotsAsync(accountId, SnapshotScenario.Actual, from, cancellationToken);

        return [.. snapshots
            .Where(snapshot => snapshot.SubPortfolioId == subPortfolioId)
            .Select(snapshot => new TwrPointDto(snapshot.Date, snapshot.TwrIndex))];
    }
}
