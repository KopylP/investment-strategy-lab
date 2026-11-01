namespace InvestmentLab.Taxation.Assessments.Domain;

public static class SalesTaxImpactCalculator
{
    public static SalesTaxImpact Of(TaxSituation situation, IReadOnlyList<TaxEvent> proposedSales)
    {
        var before = situation.YearWith([]);

        var total = ImpactOf(proposedSales, situation, before);
        var perSale = proposedSales.ToDictionary(sale => sale.TransactionId, sale => ImpactOf([sale], situation, before));

        return new SalesTaxImpact(TaxOf(before), total, perSale);
    }

    private static TaxImpact ImpactOf(IReadOnlyList<TaxEvent> sales, TaxSituation situation, TaxYearResult? before)
    {
        var after = situation.YearWith(sales);

        return new TaxImpact(
            GainInTaxCurrency: GainOf(sales, after),
            TaxChange: TaxOf(after) - TaxOf(before),
            CarriedLoss: CarriedLoss(situation.Policy, before, after));
    }

    private static decimal GainOf(IReadOnlyList<TaxEvent> sales, TaxYearResult? after)
    {
        var saleIds = sales.Select(sale => sale.TransactionId).ToHashSet();
        var realized = after?.Realizations.Where(realization => saleIds.Contains(realization.SaleTransactionId));

        return realized?.Sum(realization => realization.GainInTaxCurrency) ?? 0m;
    }

    private static decimal TaxOf(TaxYearResult? year) => year?.Summary.CapitalTax ?? 0m;

    private static decimal CarriedLoss(ITaxPolicy policy, TaxYearResult? before, TaxYearResult? after)
    {
        if (!policy.LossCarryforward.CarriesLosses)
            return 0m;

        var lossBefore = before?.Summary.LossToCarry ?? 0m;
        var lossAfter = after?.Summary.LossToCarry ?? 0m;

        return Math.Max(0m, lossAfter - lossBefore);
    }
}
