using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Domain;

/// <summary>
/// A taxable event in the account currency with the PLN rate the policy picked for it.
/// <paramref name="FxRate"/> is PLN for one unit of the account currency (1 for PLN accounts).
/// </summary>
/// <param name="AccountId">Lots are matched within one account (FIFO per securities account); the year nets all accounts.</param>
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
    Guid AccountId = default);

/// <summary>Units still held from one purchase; cost in PLN including the purchase fee.</summary>
public sealed class OpenLot(Guid buyTransactionId, DateOnly acquiredOn, int sequence, Guid instrumentId, decimal quantity, decimal cost, decimal fxRate, Guid accountId = default)
{
    public Guid AccountId { get; } = accountId;
    public Guid BuyTransactionId { get; } = buyTransactionId;
    public DateOnly AcquiredOn { get; } = acquiredOn;
    public int Sequence { get; } = sequence;
    public Guid InstrumentId { get; } = instrumentId;
    public decimal Quantity { get; private set; } = quantity;
    public decimal Cost { get; private set; } = cost;
    public decimal FxRate { get; } = fxRate;

    public void Consume(decimal quantity, decimal cost)
    {
        Quantity -= quantity;
        Cost -= cost;
    }

    public OpenLotSnapshot Snapshot() => new(BuyTransactionId, AcquiredOn, InstrumentId, Quantity, Cost, FxRate, AccountId);
}

public sealed record OpenLotSnapshot(Guid BuyTransactionId, DateOnly AcquiredOn, Guid InstrumentId, decimal Quantity, decimal CostPln, decimal FxRate, Guid AccountId = default);

public sealed record LotMatch(Guid BuyTransactionId, DateOnly AcquiredOn, decimal Quantity, decimal CostPln);

/// <summary>Gain of a sale charged to the sub-portfolio that sold.</summary>
public sealed record GainAttribution(Guid SubPortfolioId, decimal GainPln);

/// <summary>A sale matched with the lots it closed: proceeds and cost in PLN.</summary>
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
    Guid AccountId = default);

public sealed record DividendIncome(Guid TransactionId, DateOnly ReceivedOn, Guid InstrumentId, decimal Amount, decimal AmountPln, decimal FxRate, Guid AccountId = default);

/// <summary>A loss of a tax year (computed, or entered by the user for years before the journal with what was already used of it).</summary>
public sealed record LossYear(int Year, decimal Amount, decimal AlreadyUsed = 0m);

public sealed record CarryforwardUse(int LossYear, decimal Amount);

/// <summary>The year in numbers, PLN: what was sold, what it cost, what is taxable and what is due.</summary>
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
    decimal LossToCarry);

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
                    Lots(lots, entry).Add(new OpenLot(entry.TransactionId, entry.OccurredOn, entry.Sequence, entry.InstrumentId, entry.Quantity, cost, entry.FxRate, entry.AccountId));
                    break;

                case TaxableKind.Sell:
                    realizations.Add(Sell(entry, Lots(lots, entry), policy, warnings));
                    break;

                case TaxableKind.Dividend:
                    dividends.Add(new DividendIncome(entry.TransactionId, entry.OccurredOn, entry.InstrumentId, entry.Amount, Pln(entry.Amount, entry.FxRate), entry.FxRate, entry.AccountId));
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

            var uses = new List<CarryforwardUse>();
            var left = Math.Max(0m, gain);
            foreach (var pool in losses.Where(pool => pool.Loss.Year < year).OrderBy(pool => pool.Loss.Year))
            {
                if (left <= 0m)
                    break;
                var usable = Math.Min(Math.Min(policy.LossCarryforward.UsableIn(pool.Loss, year), pool.Remaining), left);
                if (usable <= 0m)
                    continue;
                pool.Use(usable);
                uses.Add(new CarryforwardUse(pool.Loss.Year, usable));
                left -= usable;
            }

            var used = uses.Sum(use => use.Amount);
            var taxBase = policy.IsTaxed ? policy.Rounding.Base(Math.Max(0m, gain - used)) : 0m;
            var capitalTax = policy.Rounding.Tax(taxBase * policy.RateSchedule.Rate.Value);
            var dividendIncome = received.Sum(d => d.AmountPln);
            var dividendTax = policy.IsTaxed ? policy.Rounding.Tax(policy.Rounding.Base(dividendIncome) * policy.RateSchedule.Rate.Value) : 0m;
            var loss = gain < 0m ? -gain : 0m;
            if (loss > 0m && policy.IsTaxed)
                losses.Add(new LossPool(new LossYear(year, loss)));

            results.Add(new TaxYearResult(
                new TaxYearSummary(year, revenue, costs, gain, used, taxBase, capitalTax, dividendIncome, dividendTax, capitalTax + dividendTax, loss),
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
        var matches = policy.LotMatcher.Take(lots, Math.Min(sale.Quantity, held));
        var unmatched = sale.Quantity - matches.Sum(match => match.Quantity);
        if (unmatched > 0m)
            warnings.Add($"Продаж {sale.OccurredOn:yyyy-MM-dd}: {unmatched} шт. без купівлі в журналі — їхня собівартість вважається нульовою.");

        var proceeds = Pln(sale.Quantity * sale.Price, sale.FxRate) - Pln(sale.Fee, sale.FxRate);
        var cost = matches.Sum(match => match.CostPln);
        var gain = proceeds - cost;

        var total = sale.Allocations.Sum(allocation => allocation.Quantity);
        var attributions = total > 0m
            ? sale.Allocations.Select(allocation => new GainAttribution(allocation.SubPortfolioId, decimal.Round(gain * allocation.Quantity / total, 2))).ToList()
            : [];

        return new Realization(sale.TransactionId, sale.OccurredOn, sale.InstrumentId, sale.Quantity, proceeds, cost, gain, sale.FxRate, sale.FxRateDate, matches, attributions, unmatched,
            sale.AccountId);
    }

    private static List<OpenLot> Lots(Dictionary<(Guid Account, Guid Instrument), List<OpenLot>> lots, TaxEvent entry) =>
        lots.TryGetValue((entry.AccountId, entry.InstrumentId), out var list) ? list : lots[(entry.AccountId, entry.InstrumentId)] = [];

    private static decimal Pln(decimal amount, decimal rate) => decimal.Round(amount * rate, 2, MidpointRounding.AwayFromZero);

    private sealed class LossPool(LossYear loss)
    {
        public LossYear Loss { get; } = loss;
        public decimal Remaining { get; private set; } = Math.Max(0m, loss.Amount - loss.AlreadyUsed);

        public void Use(decimal amount) => Remaining -= amount;
    }
}
