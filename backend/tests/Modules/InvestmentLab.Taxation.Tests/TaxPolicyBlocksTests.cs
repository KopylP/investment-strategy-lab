using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Tests;

public sealed class TaxPolicyBlocksTests
{
    private const decimal FreeAllowance = 1000m;
    private const decimal RateAboveAllowance = 0.25m;

    private static readonly Guid Etf = Guid.NewGuid(), Sub = Guid.NewGuid();

    private sealed class AllowanceSchedule : ITaxRateSchedule
    {
        public string Label => "25% понад 1000";

        public decimal TaxOn(decimal taxBase) => Math.Max(0m, taxBase - FreeAllowance) * RateAboveAllowance;
    }

    private sealed class OtherCountryPolicy : ITaxPolicy
    {
        private readonly BelkaTaxPolicy _belka = new();

        public string Code => "XX-TEST";
        public int Version => 1;
        public string Jurisdiction => "XX";
        public bool IsTaxed => true;
        public string EstimateDisclaimer => "";
        public TaxPolicyTexts Texts => _belka.Texts;
        public IFxRule FxRule => _belka.FxRule;
        public ILotMatcher LotMatcher => _belka.LotMatcher;
        public ITaxableGainRule TaxableGain => _belka.TaxableGain;
        public ITaxRateSchedule RateSchedule { get; } = new AllowanceSchedule();
        public Percentage DividendRate => Percentage.Zero;
        public ILossCarryforwardRule LossCarryforward { get; } = new NoCarryforward();
        public IRoundingRule Rounding => _belka.Rounding;
    }

    private static TaxEvent Trade(TaxableKind kind, string date, int sequence, decimal price) =>
        new(Guid.NewGuid(), DateOnly.Parse(date), sequence, kind, Etf, 10m, price, 0m, 10m * price, [new(Sub, 10m)], 1m, null);

    private sealed class NothingIsTaxable : ITaxableGainRule
    {
        public decimal TaxableGainOf(Realization sale) => 0m;
    }

    private sealed class ExemptSalesPolicy : ITaxPolicy
    {
        private readonly BelkaTaxPolicy _belka = new();

        public string Code => "XX-EXEMPT";
        public int Version => 1;
        public string Jurisdiction => "XX";
        public bool IsTaxed => true;
        public string EstimateDisclaimer => "";
        public TaxPolicyTexts Texts => _belka.Texts;
        public IFxRule FxRule => _belka.FxRule;
        public ILotMatcher LotMatcher => _belka.LotMatcher;
        public ITaxableGainRule TaxableGain { get; } = new NothingIsTaxable();
        public ITaxRateSchedule RateSchedule => _belka.RateSchedule;
        public Percentage DividendRate => _belka.DividendRate;
        public ILossCarryforwardRule LossCarryforward => _belka.LossCarryforward;
        public IRoundingRule Rounding => _belka.Rounding;
    }

    [Fact]
    public void A_gain_the_policy_exempts_shows_in_the_year_but_is_not_taxed()
    {
        TaxEvent[] events = [Trade(TaxableKind.Buy, "2025-01-10", 0, 100m), Trade(TaxableKind.Sell, "2025-06-10", 1, 500m)];

        var summary = TaxCalculator.Compute(events, new ExemptSalesPolicy(), []).Year(2025)!.Summary;

        summary.Gain.ShouldBe(4000m);
        summary.TaxBase.ShouldBe(0m);
        summary.CapitalTax.ShouldBe(0m);
    }

    [Fact]
    public void The_calculator_charges_what_the_rate_schedule_of_the_policy_says()
    {
        TaxEvent[] events = [Trade(TaxableKind.Buy, "2025-01-10", 0, 100m), Trade(TaxableKind.Sell, "2025-06-10", 1, 500m)];

        var summary = TaxCalculator.Compute(events, new OtherCountryPolicy(), []).Year(2025)!.Summary;

        summary.Gain.ShouldBe(4000m);
        summary.CapitalTax.ShouldBe(750m);
    }

    [Theory]
    [InlineData("PL-BELKA", true)]
    [InlineData("pl-belka", true)]
    [InlineData("PL-IKE", false)]
    [InlineData("PL-IKZE", false)]
    public void Known_policies_are_found_by_code(string code, bool isTaxed)
    {
        TaxPolicies.IsKnown(code).ShouldBeTrue();
        TaxPolicies.For(code).IsTaxed.ShouldBe(isTaxed);
    }

    [Theory]
    [InlineData("DE-ABGELTUNG")]
    [InlineData("")]
    [InlineData(null)]
    public void An_unknown_policy_is_refused_instead_of_quietly_taxing_as_poland(string? code)
    {
        TaxPolicies.IsKnown(code).ShouldBeFalse();
        Should.Throw<ArgumentException>(() => TaxPolicies.For(code));
    }

    [Fact]
    public void The_flat_rate_names_itself_for_the_screen()
    {
        new FlatRate(Percentage.FromPercent(19m)).Label.ShouldBe("19%");
    }
}
