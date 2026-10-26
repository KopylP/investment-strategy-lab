using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.BrokerStatements.Domain;
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
        sale.ProceedsPln.ShouldBe(8287.50m);                         // 1950 EUR × 4.25: the sale's fee is a cost (PIT-38 poz. 23)
        sale.Lots.Select(lot => lot.Quantity).ShouldBe([10m, 5m]);
        sale.CostPln.ShouldBe(4304.30m + 2642.20m + 8.50m);          // whole first lot + half of the second (5284.40 / 2) + fee 2 × 4.25
        sale.GainPln.ShouldBe(8287.50m - 6955.00m);

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
        computation.Year(2025)!.Dividends.Single().WithholdingKnown.ShouldBeFalse();   // no broker report: gross is what was received
        year.DividendTax.ShouldBe(40m);                   // 210 × 19% = 39.90
        year.TaxDue.ShouldBe(year.CapitalTax + 40m);
    }

    [Fact]
    public void Selling_more_than_bought_warns_and_ike_is_not_taxed()
    {
        var computation = TaxCalculator.Compute([Sell("2025-06-10", 5m, 100m, 0m, 4m)], new NoTaxPolicy("PL-IKE"), []);

        computation.Warnings.ShouldHaveSingleItem();
        computation.Year(2025)!.Summary.Gain.ShouldBe(2000m);
        computation.Year(2025)!.Summary.TaxDue.ShouldBe(0m);
    }

    private TaxEvent Trade(TaxableKind kind, string date, Guid instrument, decimal quantity, decimal price, decimal fee, decimal rate) =>
        new(Guid.NewGuid(), DateOnly.Parse(date), _sequence++, kind, instrument, quantity, price, fee, quantity * price, [new(Eu, quantity)], rate, null);

    private TaxEvent Dividend(string date, decimal net, decimal rate, WithholdingFacts? withholding = null) =>
        new(Guid.NewGuid(), DateOnly.Parse(date), _sequence++, TaxableKind.Dividend, Other, 0m, 0m, 0m, net, [new(Eu, 0m)], rate, null, Withholding: withholding);

    [Theory]
    [InlineData("1000.49", 1000, 190)]   // under half a złoty drops: 1000 × 19% = 190
    [InlineData("1000.50", 1001, 190)]   // half a złoty rounds up: 1001 × 19% = 190.19
    [InlineData("1002.50", 1003, 191)]   // 1003 × 19% = 190.57 rounds up
    public void Tax_base_and_tax_are_whole_zloty_half_rounding_up(string gain, int taxBase, int tax)
    {
        var computation = TaxCalculator.Compute([
            Buy("2025-01-10", 1m, 100m, 0m, 1m),
            Sell("2025-06-10", 1m, 100m + decimal.Parse(gain, System.Globalization.CultureInfo.InvariantCulture), 0m, 1m),
        ], Belka, []);

        var year = computation.Year(2025)!.Summary;
        year.TaxBase.ShouldBe(taxBase);
        year.CapitalTax.ShouldBe(tax);
    }

    [Fact]
    public void Both_fees_are_costs_each_at_the_rate_of_its_own_trade()
    {
        var computation = TaxCalculator.Compute([
            Buy("2025-01-10", 10m, 10m, 2m, 4m),     // 400 + fee 8
            Sell("2025-06-10", 10m, 20m, 3m, 5m),    // 1000, fee 15
        ], Belka, []);

        var sale = computation.Year(2025)!.Realizations.Single();
        sale.ProceedsPln.ShouldBe(1000m);
        sale.CostPln.ShouldBe(408m + 15m);
        sale.GainPln.ShouldBe(577m);
    }

    [Fact]
    public void A_partly_sold_lot_gives_a_proportional_share_of_its_cost_and_the_rest_goes_with_the_last_units()
    {
        var computation = TaxCalculator.Compute([
            Buy("2025-01-10", 3m, 10m, 1m, 1m),      // 31 for three
            Sell("2025-02-10", 1m, 20m, 0m, 1m),
            Sell("2025-03-10", 2m, 20m, 0m, 1m),
        ], Belka, []);

        computation.Year(2025)!.Realizations.Select(sale => sale.CostPln).ShouldBe([10.33m, 20.67m]);
        computation.OpenLots.ShouldBeEmpty();
    }

    [Fact]
    public void Each_instrument_has_its_own_lots()
    {
        var computation = TaxCalculator.Compute([
            Trade(TaxableKind.Buy, "2025-01-10", Etf, 10m, 100m, 0m, 1m),
            Trade(TaxableKind.Buy, "2025-02-10", Other, 10m, 50m, 0m, 1m),
            Trade(TaxableKind.Sell, "2025-06-10", Other, 5m, 60m, 0m, 1m),
        ], Belka, []);

        computation.Year(2025)!.Realizations.Single().CostPln.ShouldBe(250m);
    }

    [Fact]
    public void A_lot_bought_in_an_earlier_year_keeps_the_rate_of_its_purchase()
    {
        var computation = TaxCalculator.Compute([
            Buy("2024-03-10", 10m, 100m, 0m, 4.30m),
            Sell("2025-06-10", 10m, 100m, 0m, 4.00m),
        ], Belka, []);

        var year = computation.Year(2025)!;
        year.Realizations.Single().GainPln.ShouldBe(4000m - 4300m);   // a currency loss is a loss
        year.Summary.LossToCarry.ShouldBe(300m);
        year.Summary.TaxDue.ShouldBe(0m);
    }

    [Fact]
    public void A_loss_never_takes_more_than_the_years_gain_and_what_is_left_waits_for_the_next_years()
    {
        var computation = TaxCalculator.Compute([
            Buy("2024-01-10", 10m, 1000m, 0m, 1m),
            Sell("2024-06-10", 10m, 0m, 0m, 1m),           // loss 10000
            Buy("2025-01-10", 10m, 100m, 0m, 1m),
            Sell("2025-06-10", 10m, 200m, 0m, 1m),         // gain 1000: all of it covered
            Buy("2026-01-10", 10m, 100m, 0m, 1m),
            Sell("2026-06-10", 10m, 1100m, 0m, 1m),        // gain 10000: half of the loss, 5000
        ], Belka, []);

        computation.Year(2025)!.Summary.CarryforwardUsed.ShouldBe(1000m);
        computation.Year(2025)!.Summary.TaxBase.ShouldBe(0m);
        computation.Year(2026)!.Summary.CarryforwardUsed.ShouldBe(5000m);
        computation.Year(2026)!.Summary.TaxBase.ShouldBe(5000m);
    }

    [Fact]
    public void Older_losses_are_used_first_and_a_loss_lasts_exactly_five_years()
    {
        var losses = new[] { new LossYear(2020, 1000m), new LossYear(2023, 1000m) };
        var computation = TaxCalculator.Compute([
            Buy("2025-01-10", 1m, 100m, 0m, 1m),
            Sell("2025-06-10", 1m, 700m, 0m, 1m),          // gain 600: 500 of 2020 (its last year), 100 of 2023
            Buy("2026-01-10", 1m, 100m, 0m, 1m),
            Sell("2026-06-10", 1m, 2100m, 0m, 1m),         // gain 2000: 2020 expired, 2023 has 900 left, at most 500 a year
        ], Belka, losses);

        computation.Year(2025)!.CarryforwardUses.ShouldBe([new CarryforwardUse(2020, 500m), new CarryforwardUse(2023, 100m)]);
        computation.Year(2026)!.CarryforwardUses.ShouldBe([new CarryforwardUse(2023, 500m)]);
    }

    [Fact]
    public void Tax_paid_abroad_on_a_dividend_is_credited_only_up_to_the_polish_19_percent_of_it()
    {
        // 70 USD received of 100, 30 withheld (no W-8BEN), at 4 PLN: 19% of 400 is 76, so 76 of the 120 counts.
        var computation = TaxCalculator.Compute([Dividend("2025-05-16", 70m, 4m, new WithholdingFacts(100m / 70m, 30m / 70m))], Belka, []);

        var year = computation.Year(2025)!.Summary;
        year.DividendIncome.ShouldBe(400m);
        year.DividendTaxCredit.ShouldBe(76m);
        year.DividendTax.ShouldBe(0m);
    }

    [Fact]
    public void Dividend_tax_is_rounded_once_for_the_year_and_a_capital_loss_does_not_reduce_it()
    {
        var computation = TaxCalculator.Compute([
            Buy("2025-01-10", 1m, 1000m, 0m, 1m),
            Sell("2025-02-10", 1m, 500m, 0m, 1m),           // loss 500
            Dividend("2025-03-10", 100m, 1m),
            Dividend("2025-04-10", 100m, 1m),               // 200 × 19% = 38
            Dividend("2025-05-10", 2.70m, 1m),              // 202.70 × 19% = 38.513 → 39
        ], Belka, []);

        var year = computation.Year(2025)!.Summary;
        year.CapitalTax.ShouldBe(0m);
        year.LossToCarry.ShouldBe(500m);
        year.DividendTax.ShouldBe(39m);
        year.TaxDue.ShouldBe(39m);
    }
}
