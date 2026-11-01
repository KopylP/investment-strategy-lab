using System.Globalization;
using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Domain;

public static class TaxCalculator
{
    private const int SameLotDayTolerance = 3;
    private const decimal BrokerCostTolerance = 0.02m;
    private const decimal BrokerCostToleranceShare = 0.005m;

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
                    Lots(lots, entry).Add(LotOf(entry));
                    break;

                case TaxableKind.Sell:
                    realizations.Add(Sell(entry, Lots(lots, entry), policy, warnings));
                    break;

                case TaxableKind.Dividend when entry.Broker is { } sold:
                    realizations.Add(SaleOfIncome(entry, sold));
                    break;

                case TaxableKind.Dividend:
                    dividends.Add(DividendOf(entry));
                    break;
            }
        }

        var results = YearResults(realizations, dividends, policy, earlierLosses);
        var openLots = lots.Values.SelectMany(list => list).Select(lot => lot.Snapshot());

        return new TaxComputation(results, [.. openLots], warnings);
    }

    private static OpenLot LotOf(TaxEvent buy)
    {
        var gross = buy.Quantity * buy.Price;
        var cost = InTaxCurrency(gross, buy.FxRate) + InTaxCurrency(buy.Fee, buy.FxRate);

        return new OpenLot(buy.TransactionId, buy.OccurredOn, buy.Sequence, buy.InstrumentId, buy.Quantity, cost, buy.FxRate, gross, buy.AccountId);
    }

    private static DividendIncome DividendOf(TaxEvent entry)
    {
        var net = InTaxCurrency(entry.Amount, entry.FxRate);
        var gross = entry.Withholding is { } withholding ? InTaxCurrency(entry.Amount * withholding.GrossPerNet, entry.FxRate) : net;
        var withheld = entry.Withholding is { } paidAbroad ? InTaxCurrency(entry.Amount * paidAbroad.WithheldPerNet, entry.FxRate) : 0m;

        return new DividendIncome(
            entry.TransactionId,
            entry.OccurredOn,
            entry.InstrumentId,
            entry.Amount,
            net,
            entry.FxRate,
            gross,
            withheld,
            entry.Currency,
            entry.Withholding is not null,
            entry.AccountId);
    }

    private static List<TaxYearResult> YearResults(
        List<Realization> realizations, List<DividendIncome> dividends, ITaxPolicy policy, IReadOnlyList<LossYear> earlierLosses)
    {
        var losses = earlierLosses.Select(loss => new LossPool(loss)).ToList();
        var results = new List<TaxYearResult>();

        foreach (var year in YearsOf(realizations, dividends))
        {
            var sold = realizations.Where(realization => realization.SoldOn.Year == year).ToList();
            var received = dividends.Where(dividend => dividend.ReceivedOn.Year == year).ToList();

            results.Add(YearResult(year, sold, received, policy, losses));
        }

        return results;
    }

    private static List<int> YearsOf(List<Realization> realizations, List<DividendIncome> dividends)
    {
        var years = realizations.Select(realization => realization.SoldOn.Year)
            .Concat(dividends.Select(dividend => dividend.ReceivedOn.Year))
            .Order()
            .ToList();

        return years.Count == 0 ? [] : [.. Enumerable.Range(years[0], years[^1] - years[0] + 1)];
    }

    private static TaxYearResult YearResult(int year, List<Realization> sold, List<DividendIncome> received, ITaxPolicy policy, List<LossPool> losses)
    {
        var dividendRate = policy.DividendRate.Value;
        var revenue = sold.Sum(realization => realization.ProceedsInTaxCurrency);
        var costs = sold.Sum(realization => realization.CostInTaxCurrency);
        var gain = revenue - costs;
        var taxableGain = sold.Sum(policy.TaxableGain.TaxableGainOf);

        var uses = LossPool.UseAgainst(losses, year, taxableGain, policy.LossCarryforward);
        var used = uses.Sum(use => use.Amount);
        var taxBase = policy.IsTaxed ? policy.Rounding.Base(Math.Max(0m, taxableGain - used)) : 0m;
        var capitalTax = policy.Rounding.Tax(policy.RateSchedule.TaxOn(taxBase));

        var dividendIncome = received.Sum(dividend => dividend.GrossInTaxCurrency);
        var dividendCredit = received.Sum(dividend => Math.Min(dividend.WithheldInTaxCurrency, dividend.GrossInTaxCurrency * dividendRate));
        var dividendTax = policy.IsTaxed ? policy.Rounding.Tax(Math.Max(0m, dividendIncome * dividendRate - dividendCredit)) : 0m;

        var loss = taxableGain < 0m ? -taxableGain : 0m;
        if (loss > 0m && policy.IsTaxed)
            losses.Add(new LossPool(new LossYear(year, loss)));

        var summary = new TaxYearSummary(
            year,
            revenue,
            costs,
            gain,
            used,
            taxBase,
            capitalTax,
            dividendIncome,
            dividendTax,
            capitalTax + dividendTax,
            loss,
            policy.IsTaxed ? decimal.Round(dividendCredit, 2, MidpointRounding.AwayFromZero) : 0m);

        return new TaxYearResult(summary, sold, received, uses);
    }

    public static IReadOnlyList<TaxTimelinePoint> Timeline(
        IReadOnlyList<TaxEvent> events,
        ITaxPolicy policy,
        IReadOnlyList<LossYear> earlierLosses,
        int year)
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
            warnings.Add(Message.Of("tax.warning.sale_without_purchase", new { date = sale.OccurredOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), quantity = unmatched }));

        var proceeds = InTaxCurrency(sale.Quantity * sale.Price, sale.FxRate);
        var cost = matches.Sum(match => match.CostInTaxCurrency) + InTaxCurrency(sale.Fee, sale.FxRate);
        var gain = proceeds - cost;

        return new Realization(
            sale.TransactionId,
            sale.OccurredOn,
            sale.InstrumentId,
            sale.Quantity,
            proceeds,
            cost,
            gain,
            sale.FxRate,
            sale.FxRateDate,
            matches,
            AttributionsOf(sale, gain),
            unmatched,
            sale.Broker?.GainInTaxCurrency,
            sale.AccountId);
    }

    private static List<GainAttribution> AttributionsOf(TaxEvent entry, decimal gain)
    {
        var total = entry.Allocations.Sum(allocation => allocation.Quantity);
        if (total <= 0m)
            return [];

        return [.. entry.Allocations.Select(allocation => new GainAttribution(
            allocation.SubPortfolioId,
            decimal.Round(gain * allocation.Quantity / total, 2)))];
    }

    private static List<LotMatch> WithBrokerCost(List<LotMatch> own, SaleFacts broker, ref decimal unmatched)
    {
        var added = new List<LotMatch>();

        foreach (var lot in broker.Lots.Where(lot => !IsAmongOwn(lot, own)))
        {
            if (unmatched <= 0m)
                break;

            var taken = Math.Min(lot.Quantity, unmatched);

            added.Add(BrokerLotMatch(lot, taken));
            unmatched -= taken;
        }

        return [.. CorrectedToBrokerCost(own, added, broker), .. added];
    }

    private static bool IsAmongOwn(BrokerLotFact lot, List<LotMatch> own) =>
        own.Any(match => Math.Abs(match.AcquiredOn.DayNumber - lot.AcquiredOn.DayNumber) <= SameLotDayTolerance);

    private static LotMatch BrokerLotMatch(BrokerLotFact lot, decimal taken)
    {
        var share = taken / lot.Quantity;
        var costInTaxCurrency = decimal.Round(lot.CostInTaxCurrency * share, 2, MidpointRounding.AwayFromZero);

        return new LotMatch(Guid.Empty, lot.AcquiredOn, taken, costInTaxCurrency, FromBroker: true, Gross: lot.Cost * share);
    }

    private static List<LotMatch> CorrectedToBrokerCost(List<LotMatch> own, List<LotMatch> added, SaleFacts broker)
    {
        var ownGross = own.Sum(match => match.Gross);
        if (broker.Cost is not { } brokerCost || ownGross <= 0m)
            return own;

        var delta = brokerCost - added.Sum(match => match.Gross) - ownGross;
        if (Math.Abs(delta) <= Math.Max(BrokerCostTolerance, ownGross * BrokerCostToleranceShare))
            return own;

        return [.. own.Select(match => match with
        {
            CostInTaxCurrency = match.CostInTaxCurrency + decimal.Round(delta * match.Gross / ownGross * match.FxRate, 2, MidpointRounding.AwayFromZero),
            FromBroker = true,
        })];
    }

    private static Realization SaleOfIncome(TaxEvent income, SaleFacts sold)
    {
        var proceeds = InTaxCurrency(income.Amount, income.FxRate);
        var cost = sold.Lots.Sum(lot => lot.CostInTaxCurrency);
        var gain = proceeds - cost;
        var matches = sold.Lots.Select(lot => new LotMatch(Guid.Empty, lot.AcquiredOn, lot.Quantity, lot.CostInTaxCurrency, FromBroker: true, Gross: lot.Cost));

        return new Realization(
            income.TransactionId,
            income.OccurredOn,
            income.InstrumentId,
            sold.Lots.Sum(lot => lot.Quantity),
            proceeds,
            cost,
            gain,
            income.FxRate,
            income.FxRateDate,
            [.. matches],
            AttributionsOf(income, gain),
            0m,
            sold.GainInTaxCurrency,
            income.AccountId);
    }

    private static List<OpenLot> Lots(Dictionary<(Guid Account, Guid Instrument), List<OpenLot>> lots, TaxEvent entry) =>
        lots.TryGetValue((entry.AccountId, entry.InstrumentId), out var list) ? list : lots[(entry.AccountId, entry.InstrumentId)] = [];

    private static decimal InTaxCurrency(decimal amount, decimal rate) => decimal.Round(amount * rate, 2, MidpointRounding.AwayFromZero);
}
