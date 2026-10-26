using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Tests;

public sealed class TaxCalculatorTests
{
    private static readonly Guid Etf = Guid.NewGuid(), Other = Guid.NewGuid(), Eu = Guid.NewGuid(), Us = Guid.NewGuid();
    private static readonly BelkaTaxPolicy Belka = new();
    private int _sequence;

    private TaxEvent Buy(string date, decimal quantity, decimal price, decimal fee, decimal rate, Guid? instrument = null) =>
        new(Guid.NewGuid(), DateOnly.Parse(date), _sequence++, TaxableKind.Buy, instrument ?? Etf, quantity, price, fee, quantity * price, [new(Eu, quantity)], rate, null);

    private TaxEvent Sell(string date, decimal quantity, decimal price, decimal fee, decimal rate, params EntryAllocation[] allocations) =>
        new(Guid.NewGuid(), DateOnly.Parse(date), _sequence++, TaxableKind.Sell, Etf, quantity, price, fee, quantity * price,
            allocations.Length > 0 ? allocations : [new(Eu, quantity)], rate, null);

    [Fact]
    public void Fifo_takes_the_oldest_lots_and_converts_each_leg_at_its_own_rate()
    {
        // Bought 10 @ 100 EUR (fee 1) at 4.30, then 10 @ 120 EUR (fee 1) at 4.40; sold 15 @ 130 EUR (fee 2) at 4.25.
        var computation = TaxCalculator.Compute([
            Buy("2025-01-10", 10m, 100m, 1m, 4.30m),
            Buy("2025-03-10", 10m, 120m, 1m, 4.40m),
            Sell("2025-06-10", 15m, 130m, 2m, 4.25m),
        ], Belka, []);

        var sale = computation.Year(2025)!.Realizations.ShouldHaveSingleItem();
        sale.ProceedsPln.ShouldBe(8287.50m - 8.50m);                 // 1950 EUR × 4.25 − fee 2 × 4.25
        sale.Lots.Select(lot => lot.Quantity).ShouldBe([10m, 5m]);
        sale.CostPln.ShouldBe(4304.30m + 2642.20m);                  // whole first lot + half of the second (5284.40 / 2)
        sale.GainPln.ShouldBe(8279.00m - 6946.50m);

        var year = computation.Year(2025)!.Summary;
        year.TaxBase.ShouldBe(1333m);                                // 1332.50 → whole złoty, half rounds up
        year.CapitalTax.ShouldBe(253m);                              // 1333 × 19% = 253.27
        computation.OpenLots.ShouldHaveSingleItem().Quantity.ShouldBe(5m);
    }

    [Fact]
    public void A_loss_is_carried_forward_at_most_half_a_year_within_five_years()
    {
        var computation = TaxCalculator.Compute([
            Buy("2023-01-10", 10m, 100m, 0m, 4m),
            Sell("2023-06-10", 10m, 60m, 0m, 4m),        // loss 1600
            Buy("2024-01-10", 10m, 100m, 0m, 4m),
            Sell("2024-06-10", 10m, 200m, 0m, 4m),       // gain 4000 → at most 800 of the loss
            Buy("2025-01-10", 10m, 100m, 0m, 4m),
            Sell("2025-06-10", 10m, 150m, 0m, 4m),       // gain 2000 → the other 800
        ], Belka, []);

        computation.Year(2023)!.Summary.LossToCarry.ShouldBe(1600m);
        computation.Year(2023)!.Summary.TaxDue.ShouldBe(0m);
        computation.Year(2024)!.Summary.CarryforwardUsed.ShouldBe(800m);
        computation.Year(2024)!.Summary.TaxBase.ShouldBe(3200m);
        computation.Year(2025)!.Summary.CarryforwardUsed.ShouldBe(800m);
        computation.Year(2025)!.Summary.TaxBase.ShouldBe(1200m);
    }

    [Fact]
    public void Losses_entered_by_the_user_count_and_expire_after_five_years()
    {
        var computation = TaxCalculator.Compute([
            Buy("2025-01-10", 10m, 100m, 0m, 4m),
            Sell("2025-06-10", 10m, 200m, 0m, 4m),        // gain 4000
        ], Belka, [new LossYear(2019, 5000m), new LossYear(2022, 3000m, AlreadyUsed: 2000m)]);

        var year = computation.Year(2025)!;
        year.CarryforwardUses.ShouldBe([new CarryforwardUse(2022, 1000m)]);   // 2019 expired; 2022 has 1000 left
        year.Summary.TaxBase.ShouldBe(3000m);
    }

    [Fact]
    public void Gain_is_charged_to_the_selling_sub_portfolios_and_dividends_are_taxed_apart()
    {
        var computation = TaxCalculator.Compute([
            Buy("2025-01-10", 10m, 100m, 0m, 4m),
            Sell("2025-06-10", 10m, 110m, 0m, 4m, new EntryAllocation(Eu, 6m), new EntryAllocation(Us, 4m)),
            new TaxEvent(Guid.NewGuid(), DateOnly.Parse("2025-07-01"), _sequence++, TaxableKind.Dividend, Other, 0m, 0m, 0m, 50m, [new(Eu, 0m)], 4.2m, null),
        ], Belka, []);

        var sale = computation.Year(2025)!.Realizations.Single();
        sale.Attributions.ShouldBe([new GainAttribution(Eu, 240m), new GainAttribution(Us, 160m)]);
        var year = computation.Year(2025)!.Summary;
        year.DividendIncome.ShouldBe(210m);
        year.DividendTax.ShouldBe(40m);                   // 210 × 19% = 39.90
        year.TaxDue.ShouldBe(year.CapitalTax + 40m);
    }

    [Fact]
    public void Lots_stay_within_their_account_and_one_year_nets_all_accounts()
    {
        Guid revolut = Guid.NewGuid(), xtb = Guid.NewGuid();
        var computation = TaxCalculator.Compute([
            Buy("2025-01-10", 10m, 100m, 0m, 4m) with { AccountId = revolut },
            Buy("2025-02-10", 10m, 50m, 0m, 4m) with { AccountId = xtb },       // cheaper, but another account's lot
            Sell("2025-06-10", 10m, 120m, 0m, 4m) with { AccountId = revolut },  // gain 800 against the Revolut lot
            Sell("2025-07-10", 10m, 30m, 0m, 4m) with { AccountId = xtb },       // loss 800 against the XTB lot
        ], Belka, []);

        var year = computation.Year(2025)!;
        year.Realizations.Single(r => r.AccountId == revolut).CostPln.ShouldBe(4000m);
        year.Realizations.Single(r => r.AccountId == xtb).GainPln.ShouldBe(-800m);
        year.Summary.Gain.ShouldBe(0m);
        year.Summary.TaxDue.ShouldBe(0m);
        year.Summary.LossToCarry.ShouldBe(0m);
    }

    [Fact]
    public void Selling_more_than_bought_warns_and_ike_is_not_taxed()
    {
        var computation = TaxCalculator.Compute([Sell("2025-06-10", 5m, 100m, 0m, 4m)], new NoTaxPolicy("PL-IKE"), []);

        computation.Warnings.ShouldHaveSingleItem();
        computation.Year(2025)!.Summary.Gain.ShouldBe(2000m);
        computation.Year(2025)!.Summary.TaxDue.ShouldBe(0m);
    }
}
