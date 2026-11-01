using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Tests;

public sealed class SalesTaxImpactTests
{
    private const int Year = 2026;
    private const decimal Quantity = 10m;
    private const decimal BoughtAt = 100m;

    private static readonly Guid AlreadySold = Guid.NewGuid(), Held = Guid.NewGuid(), OtherHeld = Guid.NewGuid(), Sub = Guid.NewGuid();
    private int _sequence;

    private TaxEvent Trade(TaxableKind kind, Guid instrument, string date, decimal price) =>
        new(Guid.NewGuid(), DateOnly.Parse(date), _sequence++, kind, instrument, Quantity, price, 0m, Quantity * price, [new(Sub, Quantity)], 1m, null);

    private TaxEvent Buy(Guid instrument) => Trade(TaxableKind.Buy, instrument, "2026-01-10", BoughtAt);

    private TaxEvent Sale(Guid instrument, decimal price) => Trade(TaxableKind.Sell, instrument, "2026-06-10", price);

    private List<TaxEvent> YearWithGainOf1000() =>
    [
        Buy(AlreadySold),
        Sale(AlreadySold, 200m),
        Buy(Held),
        Buy(OtherHeld),
    ];

    private static TaxSituation Situation(List<TaxEvent> events, ITaxPolicy? policy = null, params LossYear[] losses) =>
        new(events, losses, policy ?? new BelkaTaxPolicy(), Year);

    [Fact]
    public void A_profitable_sale_raises_the_tax_of_the_year()
    {
        var sale = Sale(Held, 150m);

        var impact = SalesTaxImpactCalculator.Of(Situation(YearWithGainOf1000()), [sale]);

        impact.YearTaxBefore.ShouldBe(190m);
        impact.Total.ShouldBe(new TaxImpact(GainInTaxCurrency: 500m, TaxChange: 95m, CarriedLoss: 0m));
    }

    [Fact]
    public void A_losing_sale_lowers_the_tax_already_due_this_year()
    {
        var sale = Sale(Held, 50m);

        var impact = SalesTaxImpactCalculator.Of(Situation(YearWithGainOf1000()), [sale]);

        impact.Total.ShouldBe(new TaxImpact(GainInTaxCurrency: -500m, TaxChange: -95m, CarriedLoss: 0m));
    }

    [Fact]
    public void A_loss_above_the_gains_of_the_year_takes_the_tax_to_zero_and_the_rest_is_carried()
    {
        var bigLoss = Sale(Held, 0m);
        var secondLoss = Sale(OtherHeld, 50m);

        var impact = SalesTaxImpactCalculator.Of(Situation(YearWithGainOf1000()), [bigLoss, secondLoss]);

        impact.Total.ShouldBe(new TaxImpact(GainInTaxCurrency: -1500m, TaxChange: -190m, CarriedLoss: 500m));
    }

    [Fact]
    public void A_loss_in_a_year_without_gains_changes_no_tax_and_is_carried_whole()
    {
        var sale = Sale(Held, 50m);

        var impact = SalesTaxImpactCalculator.Of(Situation([Buy(Held)]), [sale]);

        impact.YearTaxBefore.ShouldBe(0m);
        impact.Total.ShouldBe(new TaxImpact(GainInTaxCurrency: -500m, TaxChange: 0m, CarriedLoss: 500m));
    }

    [Fact]
    public void Each_sale_is_judged_alone_while_the_total_nets_them()
    {
        var profit = Sale(Held, 200m);
        var loss = Sale(OtherHeld, 0m);

        var impact = SalesTaxImpactCalculator.Of(Situation([Buy(Held), Buy(OtherHeld)]), [profit, loss]);

        impact.PerSale[profit.TransactionId].TaxChange.ShouldBe(190m);
        impact.PerSale[loss.TransactionId].TaxChange.ShouldBe(0m);
        impact.PerSale[loss.TransactionId].CarriedLoss.ShouldBe(1000m);
        impact.Total.ShouldBe(new TaxImpact(GainInTaxCurrency: 0m, TaxChange: 0m, CarriedLoss: 0m));
    }

    [Fact]
    public void A_loss_carried_from_an_earlier_year_softens_the_rise()
    {
        var sale = Sale(Held, 200m);

        var impact = SalesTaxImpactCalculator.Of(Situation([Buy(Held)], null, new LossYear(Year - 1, 1000m)), [sale]);

        impact.Total.TaxChange.ShouldBe(95m);
    }

    [Fact]
    public void A_policy_without_carryforward_promises_no_carried_loss()
    {
        var sale = Sale(Held, 50m);

        var impact = SalesTaxImpactCalculator.Of(Situation([Buy(Held)], new NoTaxPolicy("PL-IKE")), [sale]);

        impact.Total.CarriedLoss.ShouldBe(0m);
        impact.Total.TaxChange.ShouldBe(0m);
    }
}
