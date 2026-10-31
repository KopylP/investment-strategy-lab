using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Domain;

public static class TaxCalculator
{
    public static TaxComputation Compute(IReadOnlyList<TaxEvent> events, ITaxPolicy policy, IReadOnlyList<LossYear> earlierLosses)
    {
        var lots = new Dictionary<(Guid Account, Guid Instrument), List<OpenLot>>();
        var realizations = new List<Realization>();
        var dividends = new List<DividendIncome>();
        var warnings = new List<string>();

        foreach (var entry in events.OrderBy(entry => entry.OccurredOn).ThenBy(entry => entry.Sequence))
        {
            switch (entry.Kind)
            {
                case TaxableKind.Buy:
                    var cost = Pln(entry.Quantity * entry.Price, entry.FxRate) + Pln(entry.Fee, entry.FxRate);
                    Lots(lots, entry).Add(new OpenLot(entry.TransactionId, entry.OccurredOn, entry.Sequence, entry.InstrumentId, entry.Quantity, cost, entry.FxRate,
                        entry.Quantity * entry.Price, entry.AccountId));
                    break;

                case TaxableKind.Sell:
                    realizations.Add(Sell(entry, Lots(lots, entry), policy, warnings));
                    break;

                case TaxableKind.Dividend when entry.Broker is { } sold:
                    realizations.Add(SaleOfIncome(entry, sold));
                    break;

                case TaxableKind.Dividend:
                    var net = Pln(entry.Amount, entry.FxRate);
                    var gross = entry.Withholding is { } withholding ? Pln(entry.Amount * withholding.GrossPerNet, entry.FxRate) : net;
                    var withheld = entry.Withholding is { } paidAbroad ? Pln(entry.Amount * paidAbroad.WithheldPerNet, entry.FxRate) : 0m;
                    dividends.Add(new DividendIncome(entry.TransactionId, entry.OccurredOn, entry.InstrumentId, entry.Amount, net, entry.FxRate, gross, withheld, entry.Currency,
                        entry.Withholding is not null, entry.AccountId));
                    break;
            }
        }

        var years = realizations.Select(r => r.SoldOn.Year).Concat(dividends.Select(d => d.ReceivedOn.Year)).Distinct().Order().ToList();
        if (years.Count > 0)
            years = [.. Enumerable.Range(years[0], years[^1] - years[0] + 1)];

        var losses = earlierLosses.Select(loss => new LossPool(loss)).ToList();
        var results = new List<TaxYearResult>();

        foreach (var year in years)
        {
            var sold = realizations.Where(r => r.SoldOn.Year == year).ToList();
            var received = dividends.Where(d => d.ReceivedOn.Year == year).ToList();
            var revenue = sold.Sum(r => r.ProceedsPln);
            var costs = sold.Sum(r => r.CostPln);
            var gain = revenue - costs;

            var uses = LossPool.UseAgainst(losses, year, gain, policy.LossCarryforward);
            var used = uses.Sum(use => use.Amount);
            var taxBase = policy.IsTaxed ? policy.Rounding.Base(Math.Max(0m, gain - used)) : 0m;
            var capitalTax = policy.Rounding.Tax(taxBase * policy.RateSchedule.Rate.Value);

            var rate = policy.RateSchedule.Rate.Value;
            var dividendIncome = received.Sum(d => d.GrossPln);
            var dividendCredit = received.Sum(d => Math.Min(d.WithheldPln, d.GrossPln * rate));
            var dividendTax = policy.IsTaxed ? policy.Rounding.Tax(Math.Max(0m, dividendIncome * rate - dividendCredit)) : 0m;
            var loss = gain < 0m ? -gain : 0m;
            if (loss > 0m && policy.IsTaxed)
                losses.Add(new LossPool(new LossYear(year, loss)));

            results.Add(new TaxYearResult(
                new TaxYearSummary(year, revenue, costs, gain, used, taxBase, capitalTax, dividendIncome, dividendTax, capitalTax + dividendTax, loss,
                    policy.IsTaxed ? decimal.Round(dividendCredit, 2, MidpointRounding.AwayFromZero) : 0m),
                sold,
                received,
                uses));
        }

        return new TaxComputation(results, [.. lots.Values.SelectMany(list => list).Select(lot => lot.Snapshot())], warnings);
    }

    public static IReadOnlyList<TaxTimelinePoint> Timeline(IReadOnlyList<TaxEvent> events, ITaxPolicy policy, IReadOnlyList<LossYear> earlierLosses, int year)
    {
        var days = events.Where(e => e.OccurredOn.Year == year && e.Kind is TaxableKind.Sell or TaxableKind.Dividend)
            .Select(e => e.OccurredOn).Distinct().Order();
        return [.. days.Select(day =>
        {
            var summary = Compute([.. events.Where(e => e.OccurredOn <= day)], policy, earlierLosses).Year(year)?.Summary;

            return new TaxTimelinePoint(day, summary?.Gain ?? 0m, summary?.DividendIncome ?? 0m, summary?.TaxDue ?? 0m);
        })];
    }

    private static Realization Sell(TaxEvent sale, List<OpenLot> lots, ITaxPolicy policy, List<string> warnings)
    {
        var held = lots.Sum(lot => lot.Quantity);
        List<LotMatch> matches = [.. policy.LotMatcher.Take(lots, Math.Min(sale.Quantity, held))];
        var unmatched = sale.Quantity - matches.Sum(match => match.Quantity);

        if (sale.Broker is { } broker)
            matches = WithBrokerCost(matches, broker, ref unmatched);

        if (unmatched > 0m)
            warnings.Add($"Продаж {sale.OccurredOn:yyyy-MM-dd}: {unmatched} шт. без купівлі в журналі — їхня собівартість вважається нульовою.");

        var proceeds = Pln(sale.Quantity * sale.Price, sale.FxRate);
        var cost = matches.Sum(match => match.CostPln) + Pln(sale.Fee, sale.FxRate);
        var gain = proceeds - cost;

        var total = sale.Allocations.Sum(allocation => allocation.Quantity);
        var attributions = total > 0m
            ? sale.Allocations.Select(allocation => new GainAttribution(allocation.SubPortfolioId, decimal.Round(gain * allocation.Quantity / total, 2))).ToList()
            : [];

        return new Realization(sale.TransactionId, sale.OccurredOn, sale.InstrumentId, sale.Quantity, proceeds, cost, gain, sale.FxRate, sale.FxRateDate, matches, attributions, unmatched,
            sale.Broker?.GainPln, sale.AccountId);
    }

    private static List<LotMatch> WithBrokerCost(List<LotMatch> own, SaleFacts broker, ref decimal unmatched)
    {
        var added = new List<LotMatch>();

        foreach (var lot in broker.Lots.Where(lot => !own.Any(match => Math.Abs(match.AcquiredOn.DayNumber - lot.AcquiredOn.DayNumber) <= 3)))
        {
            if (unmatched <= 0m)
                break;

            var taken = Math.Min(lot.Quantity, unmatched);
            var share = taken / lot.Quantity;
            added.Add(new LotMatch(Guid.Empty, lot.AcquiredOn, taken, decimal.Round(lot.CostPln * share, 2, MidpointRounding.AwayFromZero), FromBroker: true, Gross: lot.Cost * share));
            unmatched -= taken;
        }

        var ownGross = own.Sum(match => match.Gross);
        if (broker.Cost is { } brokerCost && ownGross > 0m)
        {
            var delta = brokerCost - added.Sum(match => match.Gross) - ownGross;
            if (Math.Abs(delta) > Math.Max(0.02m, ownGross * 0.005m))
                own = [.. own.Select(match => match with { CostPln = match.CostPln + decimal.Round(delta * match.Gross / ownGross * match.FxRate, 2, MidpointRounding.AwayFromZero), FromBroker = true })];
        }

        return [.. own, .. added];
    }

    private static Realization SaleOfIncome(TaxEvent income, SaleFacts sold)
    {
        var proceeds = Pln(income.Amount, income.FxRate);
        var cost = sold.Lots.Sum(lot => lot.CostPln);
        var gain = proceeds - cost;
        var total = income.Allocations.Sum(allocation => allocation.Quantity);
        var attributions = total > 0m
            ? income.Allocations.Select(allocation => new GainAttribution(allocation.SubPortfolioId, decimal.Round(gain * allocation.Quantity / total, 2))).ToList()
            : [];
        return new Realization(income.TransactionId, income.OccurredOn, income.InstrumentId, sold.Lots.Sum(lot => lot.Quantity), proceeds, cost, gain, income.FxRate, income.FxRateDate,
            [.. sold.Lots.Select(lot => new LotMatch(Guid.Empty, lot.AcquiredOn, lot.Quantity, lot.CostPln, FromBroker: true, Gross: lot.Cost))], attributions, 0m, sold.GainPln,
            income.AccountId);
    }

    private static List<OpenLot> Lots(Dictionary<(Guid Account, Guid Instrument), List<OpenLot>> lots, TaxEvent entry) =>
        lots.TryGetValue((entry.AccountId, entry.InstrumentId), out var list) ? list : lots[(entry.AccountId, entry.InstrumentId)] = [];

    private static decimal Pln(decimal amount, decimal rate) => decimal.Round(amount * rate, 2, MidpointRounding.AwayFromZero);
}
