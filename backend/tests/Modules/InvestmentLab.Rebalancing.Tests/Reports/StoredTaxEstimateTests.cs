using System.Text.Json;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Tests.Reports;

public sealed class StoredTaxEstimateTests
{
    [Fact]
    public void The_keys_the_migration_writes_are_the_ones_a_stored_estimate_is_read_from()
    {
        const string migrated = """
            {"policyCode":"PL-BELKA","isTaxed":true,"gainInTaxCurrency":1000,"taxChange":190,"fxRate":4.3,"notes":[],"yearTaxBefore":5,"carriedLoss":7}
            """;

        var estimate = JsonSerializer.Deserialize<TaxEstimate>(migrated, JsonSerializerOptions.Web)!;

        estimate.GainInTaxCurrency.ShouldBe(1000m);
        estimate.TaxChange.ShouldBe(190m);
        estimate.YearTaxBefore.ShouldBe(5m);
        estimate.CarriedLoss.ShouldBe(7m);
    }

    [Fact]
    public void An_estimate_made_before_the_year_totals_existed_reads_without_them()
    {
        const string older = """{"policyCode":"PL-BELKA","isTaxed":true,"gainInTaxCurrency":1,"taxChange":2,"fxRate":null,"notes":[]}""";

        var estimate = JsonSerializer.Deserialize<TaxEstimate>(older, JsonSerializerOptions.Web)!;

        estimate.YearTaxBefore.ShouldBeNull();
        estimate.CarriedLoss.ShouldBe(0m);
    }
}
