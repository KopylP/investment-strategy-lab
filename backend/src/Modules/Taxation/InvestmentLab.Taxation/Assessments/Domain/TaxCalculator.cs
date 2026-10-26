using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Domain;

/// <summary>
/// A taxable event in the currency it was settled in, with the PLN rate the policy picked for it.
/// <paramref name="FxRate"/> is PLN for one unit of that currency (1 for PLN).
/// <paramref name="Broker"/> and <paramref name="Withholding"/> come from the broker's tax report, when one was uploaded.
/// <paramref name="AccountId"/>: lots are matched within one account (FIFO per securities account); the year nets all accounts.
/// </summary>
public sealed record TaxEvent(
    Guid TransactionId,
    DateOnly OccurredOn,
    int Sequence,
    TaxableKind Kind,
    Guid InstrumentId,
    decimal Quantity,
    decimal Price,
    decimal Fee,
    decimal Amount,
    IReadOnlyList<EntryAllocation> Allocations,
    decimal FxRate,
    DateOnly? FxRateDate,
    SaleFacts? Broker = null,
    WithholdingFacts? Withholding = null,
    string? Currency = null,
    Guid AccountId = default);

/// <summary>Units still held from one purchase; cost in PLN including the purchase fee, <see cref="Gross"/> in the trade's currency without it.</summary>
public sealed class OpenLot(Guid buyTransactionId, DateOnly acquiredOn, int sequence, Guid instrumentId, decimal quantity, decimal cost, decimal fxRate, decimal gross = 0m,
    Guid accountId = default)
{
    public Guid AccountId { get; } = accountId;
    public Guid BuyTransactionId { get; } = buyTransactionId;
    public DateOnly AcquiredOn { get; } = acquiredOn;
    public int Sequence { get; } = sequence;
    public Guid InstrumentId { get; } = instrumentId;
    public decimal Quantity { get; private set; } = quantity;
    public decimal Cost { get; private set; } = cost;
    public decimal FxRate { get; } = fxRate;
    public decimal Gross { get; private set; } = gross;

    public void Consume(decimal quantity, decimal cost, decimal gross)
    {
        Quantity -= quantity;
        Cost -= cost;
        Gross -= gross;
    }

    public OpenLotSnapshot Snapshot() => new(BuyTransactionId, AcquiredOn, InstrumentId, Quantity, Cost, FxRate, AccountId);
}

public sealed record OpenLotSnapshot(Guid BuyTransactionId, DateOnly AcquiredOn, Guid InstrumentId, decimal Quantity, decimal CostPln, decimal FxRate, Guid AccountId = default);

/// <param name="FromBroker">The cost basis is the broker's: a lot the journal has no buy for (shares from a spin-off), or one whose cost the broker changed (a spin-off carved part of it out).</param>
/// <param name="Gross">Cost without the fee in the trade's currency; <paramref name="FxRate"/> is the purchase's rate.</param>
public sealed record LotMatch(Guid BuyTransactionId, DateOnly AcquiredOn, decimal Quantity, decimal CostPln, bool FromBroker = false, decimal Gross = 0m, decimal FxRate = 0m);

/// <summary>Gain of a sale charged to the sub-portfolio that sold.</summary>
public sealed record GainAttribution(Guid SubPortfolioId, decimal GainPln);

/// <summary>A sale matched with the lots it closed: proceeds (before the sale's fee) and cost (the lots' and the sale's fee) in PLN; <see cref="BrokerGainPln"/> is the broker's own figure, to compare.</summary>
public sealed record Realization(
    Guid SaleTransactionId,
    DateOnly SoldOn,
    Guid InstrumentId,
    decimal Quantity,
    decimal ProceedsPln,
    decimal CostPln,
    decimal GainPln,
    decimal FxRate,
    DateOnly? FxRateDate,
    IReadOnlyList<LotMatch> Lots,
    IReadOnlyList<GainAttribution> Attributions,
    decimal UnmatchedQuantity,
    decimal? BrokerGainPln = null,
    Guid AccountId = default);

/// <summary>
/// A dividend: <see cref="Amount"/> and <see cref="AmountPln"/> as received (net); <see cref="GrossPln"/> before the tax
/// withheld abroad (<see cref="WithheldPln"/>), known from the broker's report — without it gross is the net amount.
/// </summary>
/// <see cref="WithholdingKnown"/>: the broker's report said what was withheld; otherwise gross is only what was received.
public sealed record DividendIncome(Guid TransactionId, DateOnly ReceivedOn, Guid InstrumentId, decimal Amount, decimal AmountPln, decimal FxRate, decimal GrossPln = 0m,
    decimal WithheldPln = 0m, string? Currency = null, bool WithholdingKnown = false, Guid AccountId = default);

/// <summary>A loss of a tax year (computed, or entered by the user for years before the journal with what was already used of it).</summary>
public sealed record LossYear(int Year, decimal Amount, decimal AlreadyUsed = 0m);

public sealed record CarryforwardUse(int LossYear, decimal Amount);

/// <summary>
/// The year in numbers, PLN: what was sold, what it cost, what is taxable and what is due. <see cref="DividendIncome"/>
/// is gross; <see cref="DividendTaxCredit"/> is the tax paid abroad that reduces the Polish dividend tax.
/// </summary>
public sealed record TaxYearSummary(
    int Year,
    decimal Revenue,
    decimal Costs,
    decimal Gain,
    decimal CarryforwardUsed,
    decimal TaxBase,
    decimal CapitalTax,
    decimal DividendIncome,
    decimal DividendTax,
    decimal TaxDue,
    decimal LossToCarry,
    decimal DividendTaxCredit = 0m);

/// <summary>The year's tax as it stood at the end of <paramref name="Date"/>, PLN.</summary>
public sealed record TaxTimelinePoint(DateOnly Date, decimal Gain, decimal DividendIncome, decimal TaxDue);

public sealed record TaxYearResult(
    TaxYearSummary Summary,
    IReadOnlyList<Realization> Realizations,
    IReadOnlyList<DividendIncome> Dividends,
    IReadOnlyList<CarryforwardUse> CarryforwardUses);

public sealed record TaxComputation(IReadOnlyList<TaxYearResult> Years, IReadOnlyList<OpenLotSnapshot> OpenLots, IReadOnlyList<string> Warnings)
{
    public TaxYearResult? Year(int year) => Years.FirstOrDefault(result => result.Summary.Year == year);
}

/// <summary>
/// Pure computation over the whole history of a taxpayer (D8): lots across all years, matched within each account,
/// gains and losses of all accounts netted per year (one PIT-38), losses carried forward, every year rebuilt from
/// scratch. Same input, same result.
/// </summary>
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
            // PIT-38 part G: the rate on gross dividends, less the tax paid abroad up to that rate, rounded once at the end.
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

    /// <summary>
    /// How the tax of <paramref name="year"/> grew: the year as it stood after each day with a sale or a dividend
    /// (netting, carried losses and rounding as on that day), oldest first.
    /// </summary>
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

        // PIT-38: the revenue is the whole price; the sale's fee is a cost, like the purchase's.
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

    /// <summary>
    /// The broker's cost basis where it knows more than the journal. Units the journal never bought (a spin-off, a
    /// transfer in) take the broker's lots that none of ours stands for. Our own lots take the broker's cost in the
    /// trade's currency when it differs (a spin-off carved part of the parent's cost out), converted at each purchase's
    /// own NBP rate, with our purchase fee kept.
    /// </summary>
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
            // Under half a percent is rounding of prices and rates, not a different cost basis.
            if (Math.Abs(delta) > Math.Max(0.02m, ownGross * 0.005m))
                own = [.. own.Select(match => match with { CostPln = match.CostPln + decimal.Round(delta * match.Gross / ownGross * match.FxRate, 2, MidpointRounding.AwayFromZero), FromBroker = true })];
        }
        return [.. own, .. added];
    }

    /// <summary>
    /// Income the journal recorded for shares sold without a buy (a spin-off), which the broker reports as a sale: the
    /// income (already net of the sale fee) is the proceeds, the broker's lots are the cost.
    /// </summary>
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

/// <summary>What is left of one year's loss to deduct from later gains.</summary>
public sealed class LossPool(LossYear loss)
{
    public LossYear Loss { get; } = loss;
    public decimal Remaining { get; private set; } = Math.Max(0m, loss.Amount - loss.AlreadyUsed);

    /// <summary>Deducts the earlier losses from <paramref name="year"/>'s gain, oldest first, as far as the rule allows.</summary>
    public static IReadOnlyList<CarryforwardUse> UseAgainst(IEnumerable<LossPool> pools, int year, decimal gain, ILossCarryforwardRule rule)
    {
        var uses = new List<CarryforwardUse>();
        var left = Math.Max(0m, gain);
        foreach (var pool in pools.Where(pool => pool.Loss.Year < year).OrderBy(pool => pool.Loss.Year))
        {
            if (left <= 0m)
                break;
            var usable = Math.Min(Math.Min(rule.UsableIn(pool.Loss, year), pool.Remaining), left);
            if (usable <= 0m)
                continue;
            pool.Remaining -= usable;
            uses.Add(new CarryforwardUse(pool.Loss.Year, usable));
            left -= usable;
        }
        return uses;
    }
}
