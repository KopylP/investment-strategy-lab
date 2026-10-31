using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public static class BrokerFacts
{
    private const decimal QuantityTolerance = 0.000001m;
    private const int SaleDayTolerance = 1;
    private const int PaymentDayTolerance = 2;

    public static BrokerMatch Match(
        IReadOnlyList<TaxEvent> events,
        IReadOnlyList<BrokerSaleLot> lots,
        IReadOnlyList<BrokerDividend> dividends,
        IReadOnlyDictionary<Guid, InstrumentKey> instruments)
    {
        var sold = lots.GroupBy(lot => (lot.SoldOn, lot.Isin)).Select(group => group.ToList()).ToList();
        var sales = new Dictionary<Guid, SaleFacts>();

        MatchSales(events, sold, sales, instruments);
        MatchDividendsThatAreSales(events, sold, sales, instruments);

        var withholding = MatchWithholding(events, dividends, sales, instruments);

        return new BrokerMatch(sales, withholding);
    }

    private static void MatchSales(
        IReadOnlyList<TaxEvent> events,
        List<List<BrokerSaleLot>> sold,
        Dictionary<Guid, SaleFacts> sales,
        IReadOnlyDictionary<Guid, InstrumentKey> instruments)
    {
        foreach (var sale in events.Where(e => e.Kind == TaxableKind.Sell))
        {
            if (BestLotsFor(sale, sold, instruments) is not { } match)
                continue;

            sold.Remove(match);
            sales[sale.TransactionId] = Facts(match);
        }
    }

    private static List<BrokerSaleLot>? BestLotsFor(
        TaxEvent sale, List<List<BrokerSaleLot>> sold, IReadOnlyDictionary<Guid, InstrumentKey> instruments)
    {
        var candidates = sold
            .Where(group => DaysApart(group[0].SoldOn, sale) <= SaleDayTolerance
                            && Math.Abs(group.Sum(lot => lot.Quantity) - sale.Quantity) <= QuantityTolerance
                            && Same(group[0].Isin, group[0].Symbol, sale.InstrumentId, instruments) != false)
            .OrderByDescending(group => Same(group[0].Isin, group[0].Symbol, sale.InstrumentId, instruments) == true)
            .ThenBy(group => DaysApart(group[0].SoldOn, sale));

        return candidates.FirstOrDefault();
    }

    private static void MatchDividendsThatAreSales(
        IReadOnlyList<TaxEvent> events,
        List<List<BrokerSaleLot>> sold,
        Dictionary<Guid, SaleFacts> sales,
        IReadOnlyDictionary<Guid, InstrumentKey> instruments)
    {
        foreach (var income in events.Where(e => e.Kind == TaxableKind.Dividend))
        {
            var match = sold.FirstOrDefault(group => DaysApart(group[0].SoldOn, income) <= SaleDayTolerance
                                                     && Same(group[0].Isin, group[0].Symbol, income.InstrumentId, instruments) == true);
            if (match is null)
                continue;

            sold.Remove(match);
            sales[income.TransactionId] = Facts(match);
        }
    }

    private static Dictionary<Guid, WithholdingFacts> MatchWithholding(
        IReadOnlyList<TaxEvent> events,
        IReadOnlyList<BrokerDividend> dividends,
        Dictionary<Guid, SaleFacts> sales,
        IReadOnlyDictionary<Guid, InstrumentKey> instruments)
    {
        var withholding = new Dictionary<Guid, WithholdingFacts>();
        var paid = dividends.Where(dividend => dividend.Net > 0m).ToList();

        foreach (var dividend in events.Where(e => e.Kind == TaxableKind.Dividend && !sales.ContainsKey(e.TransactionId)))
        {
            if (BestPaymentFor(dividend, paid, instruments) is not { } match)
                continue;

            paid.Remove(match);
            withholding[dividend.TransactionId] = new WithholdingFacts(match.Gross / match.Net, match.Withheld / match.Net);
        }

        return withholding;
    }

    private static BrokerDividend? BestPaymentFor(
        TaxEvent dividend, List<BrokerDividend> paid, IReadOnlyDictionary<Guid, InstrumentKey> instruments)
    {
        var candidates = paid
            .Where(row => DaysApart(row.PaidOn, dividend) <= PaymentDayTolerance
                          && Same(row.Isin, row.Symbol, dividend.InstrumentId, instruments) != false)
            .OrderByDescending(row => Same(row.Isin, row.Symbol, dividend.InstrumentId, instruments) == true)
            .ThenBy(row => DaysApart(row.PaidOn, dividend));

        return candidates.FirstOrDefault();
    }

    private static int DaysApart(DateOnly day, TaxEvent taxEvent) => Math.Abs(day.DayNumber - taxEvent.OccurredOn.DayNumber);

    private static SaleFacts Facts(List<BrokerSaleLot> lots)
    {
        var lotFacts = lots.OrderBy(lot => lot.AcquiredOn).Select(lot => new BrokerLotFact(lot.AcquiredOn, lot.Quantity, lot.Cost, lot.CostPln));
        var gainPln = lots.Sum(lot => lot.ProceedsPln - lot.CostPln - lot.FeesPln);

        return new SaleFacts([.. lotFacts], gainPln, lots[0].Currency, lots.Sum(lot => lot.Cost));
    }

    private static bool? Same(string isin, string symbol, Guid instrumentId, IReadOnlyDictionary<Guid, InstrumentKey> instruments)
    {
        if (!instruments.TryGetValue(instrumentId, out var key))
            return null;

        if (key.Isin is { Length: > 0 } known)
            return string.Equals(known, isin, StringComparison.OrdinalIgnoreCase);

        var listed = symbol.Length > 0 && key.Symbols.Any(each => string.Equals(each.Split('.')[0], symbol, StringComparison.OrdinalIgnoreCase));

        return listed ? true : null;
    }
}
