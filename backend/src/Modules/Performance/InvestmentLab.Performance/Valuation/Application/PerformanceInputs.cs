using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Performance.Shadow.Domain;
using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Portfolio.Contracts.Transactions;

namespace InvestmentLab.Performance.Valuation.Application;

public interface IPerformanceRepository
{
    /// <summary>The account's marker, created (dirty) if there is none yet; safe when several callers race.</summary>
    Task<RecalculationMarker> MarkerAsync(Guid accountId, UserId ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RecalculationMarker>> MarkersAsync(CancellationToken cancellationToken);


    /// <summary>Replaces the account's snapshots and saves every pending change atomically.</summary>
    Task ReplaceSnapshotsAsync(Guid accountId, IReadOnlyList<ValuationSnapshot> snapshots, CancellationToken cancellationToken);


    Task<IReadOnlyList<ValuationSnapshot>> SnapshotsAsync(Guid accountId, SnapshotScenario scenario, DateOnly from, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlannedReport>> PlannedReportsAsync(Guid accountId, CancellationToken cancellationToken);

    Task<PlannedReport?> FindPlannedReportAsync(Guid reportId, CancellationToken cancellationToken);

    void AddPlannedReport(PlannedReport report);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Journal entries as sub-portfolio movements (the replay input of the valuation).</summary>
public static class MovementMapper
{
    public static IReadOnlyList<Movement> From(JournalSnapshotDto journal)
    {
        var reversed = journal.ReversedIds.ToHashSet();
        var movements = new List<Movement>();
        var sequence = 0;
        foreach (var entry in journal.Entries.Where(entry => !reversed.Contains(entry.TransactionId)))
        {
            var day = entry.OccurredOn;
            if (entry.Trade is { } trade)
            {
                var sign = trade.Side == "Buy" ? 1m : -1m;
                var total = trade.Allocations.Sum(allocation => allocation.Quantity);
                foreach (var allocation in trade.Allocations)
                {
                    var share = total > 0m ? allocation.Quantity / total : 0m;
                    // Settled in another currency: that balance pays (or receives) the broker's amount.
                    var (cashDelta, currency) = trade is { SettledAmount: { } settled, SettledCurrency: { } code }
                        ? (-sign * settled * share, code)
                        : (-sign * allocation.Quantity * trade.Price - trade.Fee * share, (string?)null);
                    movements.Add(new Movement(day, sequence++, allocation.SubPortfolioId, trade.InstrumentId, sign * allocation.Quantity,
                        cashDelta, 0m, TradePrice: trade.Price, CashCurrency: currency));
                }
            }
            else if (entry.Cash is { } cash)
            {
                var total = cash.Allocations.Sum(allocation => allocation.Amount);
                foreach (var allocation in cash.Allocations)
                {
                    var (cashDelta, flow) = cash.Kind switch
                    {
                        "Contribution" => (allocation.Amount, allocation.Amount),
                        "Withdrawal" => (-allocation.Amount, -allocation.Amount),
                        "Dividend" => (allocation.Amount, 0m),
                        _ => (-allocation.Amount, 0m),   // fees and tax payments: costs, not flows
                    };
                    // A USD top-up is a flow worth its account-currency amount that day, but the money sits in USD.
                    if (cash is { SettledAmount: { } settled, SettledCurrency: { } code } && total > 0m)
                        movements.Add(new Movement(day, sequence++, allocation.SubPortfolioId, null, 0m,
                            Math.Sign(cashDelta) * settled * allocation.Amount / total, flow, CashCurrency: code));
                    else
                        movements.Add(new Movement(day, sequence++, allocation.SubPortfolioId, null, 0m, cashDelta, flow));
                }
            }
            else if (entry.Transfer is { } transfer)
            {
                if (transfer is { InstrumentId: { } instrument, Quantity: { } quantity })
                {
                    movements.Add(new Movement(day, sequence++, transfer.FromSubPortfolioId, instrument, -quantity, 0m, 0m, TransferQuantity: -quantity));
                    movements.Add(new Movement(day, sequence++, transfer.ToSubPortfolioId, instrument, quantity, 0m, 0m, TransferQuantity: quantity));
                }
                else if (transfer.Amount is { } amount)
                {
                    movements.Add(new Movement(day, sequence++, transfer.FromSubPortfolioId, null, 0m, -amount, -amount));
                    movements.Add(new Movement(day, sequence++, transfer.ToSubPortfolioId, null, 0m, amount, amount));
                }
            }
        }
        return movements;
    }
}

/// <summary>Daily closes in the account currency (foreign quotes at the NBP cross rate of the day, via PLN).</summary>
public sealed class PriceBook(IQuoteHistory quotes, IInstrumentCatalog instruments, IFxRates fxRates)
{
    public async Task<PriceOn> LoadAsync(IReadOnlyCollection<Guid> instrumentIds, string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var catalog = (await instruments.GetInstruments(cancellationToken)).ToDictionary(instrument => instrument.Id);
        var series = (await quotes.GetQuoteHistory(instrumentIds, from.AddDays(-10), to, cancellationToken))
            .ToDictionary(item => item.InstrumentId, item => item.Points.Select(point => (point.Date, point.Close)).ToList());

        var rates = new Dictionary<string, List<(DateOnly Date, decimal Rate)>>();
        async Task<List<(DateOnly Date, decimal Rate)>> PlnSeries(string code) =>
            rates.TryGetValue(code, out var cached) ? cached
            : rates[code] = [.. (await fxRates.GetFxRateHistory(code, "PLN", from.AddDays(-15), to, cancellationToken)).Select(rate => (rate.EffectiveDate, rate.Rate))];

        var factors = new Dictionary<Guid, Func<DateOnly, decimal?>>();
        foreach (var id in instrumentIds)
        {
            var trading = catalog.TryGetValue(id, out var instrument) ? instrument.TradingCurrency : currency;
            if (trading == currency)
            {
                factors[id] = _ => 1m;
                continue;
            }
            var fromPln = trading == "PLN" ? null : await PlnSeries(trading);
            var toPln = currency == "PLN" ? null : await PlnSeries(currency);
            factors[id] = day =>
            {
                var a = fromPln is null ? 1m : Last(fromPln, day);
                var b = toPln is null ? 1m : Last(toPln, day);
                return a is { } x && b is { } y && y > 0m ? x / y : null;
            };
        }

        return (instrumentId, day) =>
            series.TryGetValue(instrumentId, out var points) && Last(points, day) is { } close && factors[instrumentId](day) is { } factor
                ? close * factor
                : null;
    }

    /// <summary>Daily NBP cross rates of other currencies to the account currency, for balances kept in them.</summary>
    public async Task<FxOn> LoadFxAsync(IReadOnlyCollection<string> currencies, string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        async Task<List<(DateOnly Date, decimal Rate)>?> PlnSeries(string code) => code == "PLN"
            ? null
            : [.. (await fxRates.GetFxRateHistory(code, "PLN", from.AddDays(-15), to, cancellationToken)).Select(rate => (rate.EffectiveDate, rate.Rate))];

        var toPln = await PlnSeries(currency);
        var series = new Dictionary<string, List<(DateOnly Date, decimal Rate)>?>();
        foreach (var code in currencies.Distinct())
            series[code] = await PlnSeries(code);

        return (code, day) =>
        {
            if (code == currency)
                return 1m;
            if (!series.TryGetValue(code, out var fromPln))
                return null;
            var a = fromPln is null ? 1m : Last(fromPln, day);
            var b = toPln is null ? 1m : Last(toPln, day);
            return a is { } x && b is { } y && y > 0m ? x / y : null;
        };
    }

    private static decimal? Last(List<(DateOnly Date, decimal Value)> points, DateOnly day)
    {
        decimal? found = null;
        foreach (var (date, value) in points)
        {
            if (date > day)
                break;
            found = value;
        }
        return found;
    }
}
