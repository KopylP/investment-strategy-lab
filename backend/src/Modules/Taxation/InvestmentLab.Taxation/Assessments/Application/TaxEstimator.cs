using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Contracts;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Application;

/// <summary>
/// Tax of planned sales: the taxpayer's year (all taxed accounts, one PIT-38) is computed with and without them, the
/// difference is the estimate — so gains and losses realised earlier in the year in any account and carried losses count.
/// </summary>
public sealed class TaxEstimator(TaxInputs inputs, IAssessmentRepository assessments) : ITaxEstimator
{
    public async Task<TaxEstimateDto> EstimateSalesAsync(Guid accountId, IReadOnlyList<ProposedSaleDto> sales, DateOnly asOf, CancellationToken cancellationToken)
    {
        // Catching the ledger copies up writes, so it queues behind recomputations of the same taxpayer.
        var taxpayer = await inputs.OwnerOfAsync(accountId, cancellationToken) is { } ownerId
            ? await assessments.SerializedAsync(ownerId, () => inputs.TaxpayerAsync(ownerId, cancellationToken), cancellationToken)
            : null;
        if (taxpayer?.Accounts.FirstOrDefault(candidate => candidate.AccountId == accountId) is not { } account)
            return new TaxEstimateDto("", false, 0m, 0m, null, [], ["Рахунок не знайдено."]);

        var policy = TaxPolicies.For(account.PolicyCode);
        if (!policy.IsTaxed)
            return new TaxEstimateDto(policy.Code, false, 0m, 0m, null, [.. sales.Select(sale => new SaleEstimateDto(sale.Key, 0m, 0m))], []);

        var (events, missing) = await inputs.EventsAsync(taxpayer.Taxed, cancellationToken);
        var losses = await inputs.LossesAsync(account.OwnerId, cancellationToken);
        var (rate, rateDate) = await inputs.RateAsync(account.Currency, asOf.AddDays(1), cancellationToken);
        var notes = new List<string> { "Оцінка: остаточний податок — за PIT-8C брокера." };
        if (missing.Count > 0)
            notes.Add("Частина операцій без курсу NBP не врахована.");
        if (rate is not { } fx || sales.Count == 0)
            return new TaxEstimateDto(policy.Code, policy.IsTaxed, 0m, 0m, rate, [.. sales.Select(sale => new SaleEstimateDto(sale.Key, 0m, 0m))],
                rate is null ? [.. notes, "Немає курсу NBP на сьогодні — податок не оцінено."] : notes);

        var baseline = YearTax(TaxCalculator.Compute(events, policy, losses), asOf.Year);
        var sequence = events.Count;
        TaxEvent Sale(ProposedSaleDto sale) => new(sale.Key, asOf, sequence++, TaxableKind.Sell, sale.InstrumentId, sale.Quantity, sale.Price, 0m,
            sale.Quantity * sale.Price, [.. sale.SubPortfolioIds.Select(id => new EntryAllocation(id, sale.Quantity / Math.Max(1, sale.SubPortfolioIds.Count)))], fx, rateDate, AccountId: accountId);

        var all = TaxCalculator.Compute([.. events, .. sales.Select(Sale)], policy, losses);
        var perSale = new List<SaleEstimateDto>();
        foreach (var sale in sales)
        {
            var alone = TaxCalculator.Compute([.. events, Sale(sale)], policy, losses);
            var gain = alone.Year(asOf.Year)?.Realizations.FirstOrDefault(r => r.SaleTransactionId == sale.Key)?.GainPln ?? 0m;
            perSale.Add(new SaleEstimateDto(sale.Key, gain, Math.Max(0m, YearTax(alone, asOf.Year) - baseline)));
        }

        var totalGain = all.Year(asOf.Year)?.Realizations.Where(r => sales.Any(sale => sale.Key == r.SaleTransactionId)).Sum(r => r.GainPln) ?? 0m;
        return new TaxEstimateDto(policy.Code, policy.IsTaxed, totalGain, Math.Max(0m, YearTax(all, asOf.Year) - baseline), fx, perSale, notes);
    }

    private static decimal YearTax(TaxComputation computation, int year) => computation.Year(year)?.Summary.CapitalTax ?? 0m;
}
