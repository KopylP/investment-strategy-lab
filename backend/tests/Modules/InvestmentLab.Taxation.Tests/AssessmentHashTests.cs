using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Tests;

public sealed class AssessmentHashTests
{
    private const string Golden = "FB25B5679531085473BB30E7FC471909796558D33AC146FC0C02D51A72B4D845";

    private static readonly Guid Id = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly IReadOnlyList<LossYear> NoLosses = [];

    private static TaxEvent EventOn(int year) => new(
        TransactionId: Id,
        OccurredOn: new DateOnly(year, 3, 2),
        Sequence: 0,
        Kind: TaxableKind.Buy,
        InstrumentId: Id,
        Quantity: 2m,
        Price: 10m,
        Fee: 1m,
        Amount: 20m,
        Allocations: [new EntryAllocation(Id, 2m)],
        FxRate: 4.5m,
        FxRateDate: new DateOnly(year, 3, 1),
        Currency: "EUR",
        AccountId: Id);

    [Fact]
    public void The_hash_of_a_year_stays_the_same_so_stored_years_are_not_recomputed_without_need()
    {
        AssessmentHash.For(2025, [EventOn(2025)], NoLosses, TaxPolicies.For(BelkaTaxPolicy.PolicyCode)).ShouldBe(Golden);
    }

    [Fact]
    public void Events_after_the_year_do_not_change_its_hash()
    {
        var policy = TaxPolicies.For(BelkaTaxPolicy.PolicyCode);

        AssessmentHash.For(2025, [EventOn(2025), EventOn(2026)], NoLosses, policy)
            .ShouldBe(AssessmentHash.For(2025, [EventOn(2025)], NoLosses, policy));
    }

    [Fact]
    public void An_earlier_event_changes_the_hash_of_the_year()
    {
        var policy = TaxPolicies.For(BelkaTaxPolicy.PolicyCode);

        AssessmentHash.For(2025, [EventOn(2024), EventOn(2025)], NoLosses, policy)
            .ShouldNotBe(AssessmentHash.For(2025, [EventOn(2025)], NoLosses, policy));
    }

    [Fact]
    public void Another_policy_changes_the_hash()
    {
        AssessmentHash.For(2025, [EventOn(2025)], NoLosses, TaxPolicies.For(BelkaTaxPolicy.PolicyCode))
            .ShouldNotBe(AssessmentHash.For(2025, [EventOn(2025)], NoLosses, new NoTaxPolicy("PL-IKE")));
    }
}
