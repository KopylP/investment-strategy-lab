using InvestmentLab.Performance.Shadow.Domain;
using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Valuation.Application.Shared;

public sealed class SnapshotBuilder(IAccountJournals journals, PriceBook prices, IPerformanceRepository store, TimeProvider clock)
{
    public async Task<Result> EnsureFreshAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (await journals.FindAsync(accountId, cancellationToken) is not { } journal)
            return Result.Success();

        var marker = await store.MarkerAsync(accountId, journal.OwnerId, cancellationToken);

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var through = LastBusinessDay(today);
        if (!marker.IsDirty && marker.BuiltThrough >= through)
            return Result.Success();

        var valued = await ValueAsync(accountId, journal, through, cancellationToken);
        if (valued.IsFailure)
        {
            await store.ReplaceSnapshotsAsync(accountId, [], cancellationToken);

            return valued.Error;
        }

        marker.Built(clock.GetUtcNow(), through);
        await store.ReplaceSnapshotsAsync(accountId, valued.Value, cancellationToken);

        return Result.Success();
    }

    private async Task<Result<IReadOnlyList<ValuationSnapshot>>> ValueAsync(
        Guid accountId, AccountJournal journal, DateOnly through, CancellationToken cancellationToken)
    {
        var movements = JournalMovements.Of(journal);
        if (movements.Count == 0)
            return Result<IReadOnlyList<ValuationSnapshot>>.Success([]);

        var planned = await store.PlannedReportsAsync(accountId, cancellationToken);
        var shadow = ShadowPortfolioSimulator.Movements(movements, planned);
        var from = movements.Min(movement => movement.Date);
        var days = BusinessDays(from, through);
        var both = movements.Concat(shadow).ToList();
        var priceOn = await prices.LoadAsync(InstrumentsOf(both), journal.Currency, from, through, cancellationToken);
        var fxOn = await prices.LoadFxAsync(CashCurrenciesOf(both), journal.Currency, from, through, cancellationToken);

        var actual = ValuationCalculator.Build(movements, days, priceOn, fxOn);
        if (actual.IsFailure)
            return actual.Error;

        var fullPlan = ValuationCalculator.Build(shadow, days, priceOn, fxOn);
        if (fullPlan.IsFailure)
            return fullPlan.Error;

        return SnapshotsOf(accountId, journal.OwnerId, actual.Value, AccountTotals(fullPlan.Value));
    }

    private static List<Guid> InstrumentsOf(List<Movement> movements) =>
        [.. movements.Select(movement => movement.InstrumentId).OfType<Guid>().Distinct()];

    private static List<string> CashCurrenciesOf(List<Movement> movements) =>
        [.. movements.Select(movement => movement.CashCurrency).OfType<string>().Distinct()];

    private static IEnumerable<Snapshot> AccountTotals(IReadOnlyList<Snapshot> snapshots) =>
        snapshots.Where(snapshot => snapshot.SubPortfolioId is null);

    private static List<ValuationSnapshot> SnapshotsOf(Guid accountId, UserId owner, IReadOnlyList<Snapshot> actual, IEnumerable<Snapshot> fullPlan)
    {
        var actualSnapshots = actual.Select(snapshot => ValuationSnapshot.From(accountId, owner, SnapshotScenario.Actual, snapshot));
        var fullPlanSnapshots = fullPlan.Select(snapshot => ValuationSnapshot.From(accountId, owner, SnapshotScenario.FullPlan, snapshot));

        return [.. actualSnapshots, .. fullPlanSnapshots];
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
