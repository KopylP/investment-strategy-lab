using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.BrokerStatements.Domain;

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

    private static bool? Same(string isin, string symbol, Guid instrumentId, IReadOnlyDictionary<Guid, InstrumentKey> instruments)
    {
        if (!instruments.TryGetValue(instrumentId, out var key))
            return null;

        if (key.Isin is { Length: > 0 } known)
            return string.Equals(known, isin, StringComparison.OrdinalIgnoreCase);

        return symbol.Length > 0 && key.Symbols.Any(listed => string.Equals(listed.Split('.')[0], symbol, StringComparison.OrdinalIgnoreCase)) ? true : null;
    }
}
