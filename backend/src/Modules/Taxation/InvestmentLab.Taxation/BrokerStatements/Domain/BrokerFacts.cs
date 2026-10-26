using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.BrokerStatements.Domain;

/// <summary>
/// A lot as the broker matched it to a sale: cost without fees in the security's currency, and in PLN — the broker's
/// figure until the tax inputs restate it at the NBP rate of the day before the purchase.
/// </summary>
public sealed record BrokerLotFact(DateOnly AcquiredOn, decimal Quantity, decimal Cost, decimal CostPln);

/// <summary>
/// The broker's view of one sale of the journal: the lots it closed and its gain after fees, PLN. Also attached to an
/// income entry that the broker reports as a sale (shares from a spin-off the CSV import could only record as income).
/// </summary>
/// <param name="Cost">
/// The broker's cost basis of the whole sale in <paramref name="Currency"/> — it knows corporate actions the journal does
/// not (a spin-off carves part of the parent's cost out). Null when the journal entry is in another currency.
/// </param>
public sealed record SaleFacts(IReadOnlyList<BrokerLotFact> Lots, decimal GainPln, string Currency, decimal? Cost);

/// <summary>A dividend of the journal (recorded net) before the tax withheld abroad, as multiples of the net amount.</summary>
public sealed record WithholdingFacts(decimal GrossPerNet, decimal WithheldPerNet);

/// <summary>What identifies an instrument of the catalog in a broker's report.</summary>
public sealed record InstrumentKey(string? Isin, IReadOnlyList<string> Symbols);

public sealed record BrokerMatch(IReadOnlyDictionary<Guid, SaleFacts> Sales, IReadOnlyDictionary<Guid, WithholdingFacts> Dividends);

/// <summary>
/// Pairs the rows of a broker's report with the journal's sales and dividends: by day and quantity (a sale) or day and
/// amount (a dividend), never by the names in the report. ISIN or ticker only breaks ties and rules out another
/// instrument.
/// </summary>
public static class BrokerFacts
{
    private const decimal QuantityTolerance = 0.000001m;

    public static BrokerMatch Match(
        IReadOnlyList<TaxEvent> events, IReadOnlyList<BrokerSaleLot> lots, IReadOnlyList<BrokerDividend> dividends, IReadOnlyDictionary<Guid, InstrumentKey> instruments)
    {
        var sales = new Dictionary<Guid, SaleFacts>();
        var sold = lots.GroupBy(lot => (lot.SoldOn, lot.Isin)).Select(group => group.ToList()).ToList();
        foreach (var sale in events.Where(e => e.Kind == TaxableKind.Sell))
        {
            var candidates = sold
                .Where(group => Math.Abs(group[0].SoldOn.DayNumber - sale.OccurredOn.DayNumber) <= 1
                                && Math.Abs(group.Sum(lot => lot.Quantity) - sale.Quantity) <= QuantityTolerance
                                && Same(group[0].Isin, group[0].Symbol, sale.InstrumentId, instruments) != false)
                .OrderByDescending(group => Same(group[0].Isin, group[0].Symbol, sale.InstrumentId, instruments) == true)
                .ThenBy(group => Math.Abs(group[0].SoldOn.DayNumber - sale.OccurredOn.DayNumber))
                .ToList();
            if (candidates.FirstOrDefault() is not { } match)
                continue;
            sold.Remove(match);
            sales[sale.TransactionId] = Facts(match);
        }

        // Shares sold without a buy in the journal (a spin-off) were imported as income; the broker reports a sale with
        // a cost. Only a confirmed instrument (ISIN or ticker) turns income into a sale.
        foreach (var income in events.Where(e => e.Kind == TaxableKind.Dividend))
        {
            if (sold.FirstOrDefault(group => Math.Abs(group[0].SoldOn.DayNumber - income.OccurredOn.DayNumber) <= 1
                                             && Same(group[0].Isin, group[0].Symbol, income.InstrumentId, instruments) == true) is not { } match)
                continue;
            sold.Remove(match);
            sales[income.TransactionId] = Facts(match);
        }

        var withholding = new Dictionary<Guid, WithholdingFacts>();
        var paid = dividends.Where(dividend => dividend.Net > 0m).ToList();
        foreach (var dividend in events.Where(e => e.Kind == TaxableKind.Dividend && !sales.ContainsKey(e.TransactionId)))
        {
            var candidates = paid
                .Where(row => Math.Abs(row.PaidOn.DayNumber - dividend.OccurredOn.DayNumber) <= 2 && Same(row.Isin, row.Symbol, dividend.InstrumentId, instruments) != false)
                .OrderByDescending(row => Same(row.Isin, row.Symbol, dividend.InstrumentId, instruments) == true)
                .ThenBy(row => Math.Abs(row.PaidOn.DayNumber - dividend.OccurredOn.DayNumber))
                .ToList();
            if (candidates.FirstOrDefault() is not { } match)
                continue;
            paid.Remove(match);
            withholding[dividend.TransactionId] = new WithholdingFacts(match.Gross / match.Net, match.Withheld / match.Net);
        }

        return new BrokerMatch(sales, withholding);
    }

    private static SaleFacts Facts(List<BrokerSaleLot> lots) =>
        new([.. lots.OrderBy(lot => lot.AcquiredOn).Select(lot => new BrokerLotFact(lot.AcquiredOn, lot.Quantity, lot.Cost, lot.CostPln))],
            lots.Sum(lot => lot.ProceedsPln - lot.CostPln - lot.FeesPln), lots[0].Currency, lots.Sum(lot => lot.Cost));

    /// <summary>
    /// True: the same instrument; false: certainly another one (a different ISIN); null: cannot tell. Tickers only
    /// confirm: the user may have mapped the broker's ticker to a differently named listing.
    /// </summary>
    private static bool? Same(string isin, string symbol, Guid instrumentId, IReadOnlyDictionary<Guid, InstrumentKey> instruments)
    {
        if (!instruments.TryGetValue(instrumentId, out var key))
            return null;
        if (key.Isin is { Length: > 0 } known)
            return string.Equals(known, isin, StringComparison.OrdinalIgnoreCase);
        // A Yahoo symbol carries the exchange ("ASME.DE"); the broker writes the bare ticker ("ASME").
        return symbol.Length > 0 && key.Symbols.Any(listed => string.Equals(listed.Split('.')[0], symbol, StringComparison.OrdinalIgnoreCase)) ? true : null;
    }
}
