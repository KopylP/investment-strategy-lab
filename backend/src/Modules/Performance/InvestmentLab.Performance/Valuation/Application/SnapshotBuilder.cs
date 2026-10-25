using InvestmentLab.Performance.Shadow.Domain;
using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Portfolio.Contracts.Transactions;

namespace InvestmentLab.Performance.Valuation.Application;

/// <summary>
/// Rebuilds an account's daily snapshots, real and "everything as planned" (§6.8 RebuildSnapshots), from the journal
/// and quotes. Runs when the marker is dirty or the snapshots end before the last trading day.
/// </summary>
public sealed class SnapshotBuilder(IJournalQueries journal, PriceBook prices, IPerformanceRepository store, TimeProvider clock)
{
    public async Task<RecalculationMarker?> EnsureFreshAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (await journal.GetJournal(accountId, cancellationToken) is not { } snapshot)
            return null;

        var marker = await store.MarkerAsync(accountId, UserId.Of(snapshot.OwnerId), cancellationToken);

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var through = LastBusinessDay(today);
        if (!marker.IsDirty && marker.BuiltThrough >= through)
            return marker;

        var movements = MovementMapper.From(snapshot);
        var planned = await store.PlannedReportsAsync(accountId, cancellationToken);
        var shadow = ShadowPortfolioSimulator.Movements(movements, planned);
        var stored = new List<ValuationSnapshot>();
        if (movements.Count > 0)
        {
            var from = movements.Min(movement => movement.Date);
            var days = BusinessDays(from, through);
            var instruments = movements.Concat(shadow).Select(movement => movement.InstrumentId).OfType<Guid>().Distinct().ToList();
            var priceOn = await prices.LoadAsync(instruments, snapshot.Currency, from, through, cancellationToken);
            var currencies = movements.Select(movement => movement.CashCurrency).OfType<string>().ToList();
            var fxOn = await prices.LoadFxAsync(currencies, snapshot.Currency, from, through, cancellationToken);
            var owner = UserId.Of(snapshot.OwnerId);
            stored.AddRange(ValuationCalculator.Build(movements, days, priceOn, fxOn).Select(s => ValuationSnapshot.From(accountId, owner, SnapshotScenario.Actual, s)));
            stored.AddRange(ValuationCalculator.Build(shadow, days, priceOn, fxOn).Where(s => s.SubPortfolioId is null)
                .Select(s => ValuationSnapshot.From(accountId, owner, SnapshotScenario.FullPlan, s)));
        }

        marker.Built(clock.GetUtcNow(), through);
        await store.ReplaceSnapshotsAsync(accountId, stored, cancellationToken);
        return marker;
    }

    private static DateOnly LastBusinessDay(DateOnly day)
    {
        while (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            day = day.AddDays(-1);
        return day;
    }

    private static List<DateOnly> BusinessDays(DateOnly from, DateOnly to)
    {
        var days = new List<DateOnly>();
        for (var day = from; day <= to; day = day.AddDays(1))
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
                days.Add(day);
        if (days.Count == 0)
            days.Add(to);
        return days;
    }
}
