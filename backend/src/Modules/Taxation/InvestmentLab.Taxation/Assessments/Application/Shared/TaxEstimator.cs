using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Contracts;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed class TaxEstimator(TaxInputs inputs, IAssessmentRepository assessments) : ITaxEstimator
{
    public async Task<TaxEstimateDto> EstimateSalesAsync(
        Guid accountId,
        IReadOnlyList<ProposedSaleDto> sales,
        DateOnly asOf,
        CancellationToken cancellationToken)
    {
        var taxpayer = await TaxpayerOfAsync(accountId, cancellationToken);
        if (taxpayer?.Accounts.FirstOrDefault(candidate => candidate.AccountId == accountId) is not { } account)
            return new TaxEstimateDto("", false, 0m, 0m, null, [], ["Рахунок не знайдено."]);

        var policy = TaxPolicies.For(account.PolicyCode);
        if (!policy.IsTaxed)
            return new TaxEstimateDto(policy.Code, false, 0m, 0m, null, NothingFor(sales), []);

        var (events, missing) = await inputs.EventsAsync(taxpayer.Taxed, cancellationToken);
        var losses = await inputs.LossesAsync(account.OwnerId, cancellationToken);
        var (rate, rateDate) = await inputs.LatestRateAsync(account.Currency, asOf, policy.FxRule, cancellationToken);
        var notes = NotesOf(policy, missing);

        if (rate is not { } fx || sales.Count == 0)
            return WithoutEstimate(policy, rate, sales, notes);

        var scenario = new SaleScenario(accountId, asOf, events, losses, policy, fx, rateDate);

        return Estimate(scenario, sales, notes);
    }

    private async Task<Taxpayer?> TaxpayerOfAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (await inputs.OwnerOfAsync(accountId, cancellationToken) is not { } ownerId)
            return null;

        return await assessments.SerializedAsync(ownerId, () => inputs.TaxpayerAsync(ownerId, cancellationToken), cancellationToken);
    }

    private static List<SaleEstimateDto> NothingFor(IReadOnlyList<ProposedSaleDto> sales) =>
        [.. sales.Select(sale => new SaleEstimateDto(sale.Key, 0m, 0m))];

    private static List<string> NotesOf(ITaxPolicy policy, IReadOnlyList<string> missing)
    {
        var notes = new List<string> { policy.EstimateDisclaimer };

        if (missing.Count > 0)
            notes.Add($"Частина операцій без курсу {policy.FxRule.SourceName} не врахована.");

        return notes;
    }

    private static TaxEstimateDto WithoutEstimate(ITaxPolicy policy, decimal? rate, IReadOnlyList<ProposedSaleDto> sales, List<string> notes)
    {
        var shown = rate is null ? [.. notes, $"Немає курсу {policy.FxRule.SourceName} на сьогодні — податок не оцінено."] : notes;

        return new TaxEstimateDto(policy.Code, policy.IsTaxed, 0m, 0m, rate, NothingFor(sales), shown);
    }

    private static TaxEstimateDto Estimate(SaleScenario scenario, IReadOnlyList<ProposedSaleDto> sales, List<string> notes)
    {
        var year = scenario.AsOf.Year;
        var baseline = YearTax(scenario.Compute([]), year);
        var all = scenario.Compute(sales);

        var perSale = sales.Select(sale => EstimateOf(sale, scenario, baseline)).ToList();

        var realizations = all.Year(year)?.Realizations.Where(realization => sales.Any(sale => sale.Key == realization.SaleTransactionId));
        var totalGain = realizations?.Sum(realization => realization.GainPln) ?? 0m;
        var totalTax = Math.Max(0m, YearTax(all, year) - baseline);

        return new TaxEstimateDto(scenario.Policy.Code, scenario.Policy.IsTaxed, totalGain, totalTax, scenario.Rate, perSale, notes);
    }

    private static SaleEstimateDto EstimateOf(ProposedSaleDto sale, SaleScenario scenario, decimal baseline)
    {
        var year = scenario.AsOf.Year;
        var alone = scenario.Compute([sale]);
        var realization = alone.Year(year)?.Realizations.FirstOrDefault(each => each.SaleTransactionId == sale.Key);

        return new SaleEstimateDto(sale.Key, realization?.GainPln ?? 0m, Math.Max(0m, YearTax(alone, year) - baseline));
    }

    private static decimal YearTax(TaxComputation computation, int year) => computation.Year(year)?.Summary.CapitalTax ?? 0m;

    private sealed record SaleScenario(
        Guid AccountId,
        DateOnly AsOf,
        IReadOnlyList<TaxEvent> Events,
        IReadOnlyList<LossYear> Losses,
        ITaxPolicy Policy,
        decimal Rate,
        DateOnly? RateDate)
    {
        public TaxComputation Compute(IEnumerable<ProposedSaleDto> sales)
        {
            var proposed = sales.Select((sale, index) => EventOf(sale, Events.Count + index));

            return TaxCalculator.Compute([.. Events, .. proposed], Policy, Losses);
        }

        private TaxEvent EventOf(ProposedSaleDto sale, int sequence)
        {
            var shareOfSub = sale.Quantity / Math.Max(1, sale.SubPortfolioIds.Count);
            var allocations = sale.SubPortfolioIds.Select(id => new EntryAllocation(id, shareOfSub));

            return new TaxEvent(
                sale.Key,
                AsOf,
                sequence,
                TaxableKind.Sell,
                sale.InstrumentId,
                sale.Quantity,
                sale.Price,
                0m,
                sale.Quantity * sale.Price,
                [.. allocations],
                Rate,
                RateDate,
                AccountId: AccountId);
        }
    }
}
