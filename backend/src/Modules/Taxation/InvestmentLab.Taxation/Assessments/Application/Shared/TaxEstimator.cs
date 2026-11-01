using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Contracts;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed class TaxEstimator(
    IAccountSetupQueries accounts,
    TaxpayerReader taxpayers,
    TaxEventReader taxEvents,
    IAssessmentRepository assessments) : ITaxEstimator
{
    public async Task<TaxEstimateDto> EstimateSalesAsync(
        Guid accountId,
        IReadOnlyList<ProposedSaleDto> sales,
        DateOnly asOf,
        CancellationToken cancellationToken)
    {
        var taxpayer = await TaxpayerOfAsync(accountId, cancellationToken);
        if (taxpayer?.Accounts.FirstOrDefault(candidate => candidate.AccountId == accountId) is not { } account)
            return new TaxEstimateDto("", false, 0m, 0m, null, [], [Message.Of("tax.estimate.account_missing")]);

        var policy = TaxPolicies.For(account.PolicyCode);
        if (!policy.IsTaxed)
            return new TaxEstimateDto(policy.Code, false, 0m, 0m, null, NothingFor(sales), []);

        var (events, missing) = await taxEvents.EventsAsync(taxpayer.Taxed, cancellationToken);
        var losses = await taxpayers.LossesAsync(account.OwnerId, cancellationToken);
        var (rate, rateDate) = await taxEvents.LatestRateAsync(account.Currency, asOf, policy.FxRule, cancellationToken);
        var notes = NotesOf(policy, missing);

        if (rate is not { } fx || sales.Count == 0)
            return WithoutEstimate(policy, rate, sales, notes);

        var scenario = new SaleScenario(accountId, asOf, events, losses, policy, fx, rateDate);

        return Estimate(scenario, sales, notes);
    }

    private async Task<Taxpayer?> TaxpayerOfAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (await accounts.GetAccountSetup(accountId, cancellationToken) is not { } setup)
            return null;

        var ownerId = UserId.Of(setup.OwnerId);

        return await assessments.SerializedAsync(ownerId, () => taxpayers.ReadAsync(ownerId, cancellationToken), cancellationToken);
    }

    private static List<SaleEstimateDto> NothingFor(IReadOnlyList<ProposedSaleDto> sales) =>
        [.. sales.Select(sale => new SaleEstimateDto(sale.Key, 0m, 0m))];

    private static List<string> NotesOf(ITaxPolicy policy, IReadOnlyList<string> missing)
    {
        var notes = new List<string> { policy.EstimateDisclaimer };

        if (missing.Count > 0)
            notes.Add(Message.Of("tax.estimate.some_rates_missing", new { source = policy.FxRule.SourceName }));

        return notes;
    }

    private static TaxEstimateDto WithoutEstimate(ITaxPolicy policy, decimal? rate, IReadOnlyList<ProposedSaleDto> sales, List<string> notes)
    {
        var shown = rate is null ? [.. notes, Message.Of("tax.estimate.rate_missing", new { source = policy.FxRule.SourceName })] : notes;

        return new TaxEstimateDto(policy.Code, policy.IsTaxed, 0m, 0m, rate, NothingFor(sales), shown, TaxCurrency: policy.FxRule.TaxCurrency);
    }

    private static TaxEstimateDto Estimate(SaleScenario scenario, IReadOnlyList<ProposedSaleDto> sales, List<string> notes)
    {
        var proposed = sales.Select(scenario.EventOf).ToList();
        var situation = new TaxSituation(scenario.Events, scenario.Losses, scenario.Policy, scenario.AsOf.Year);

        var impact = SalesTaxImpactCalculator.Of(situation, proposed);

        return new TaxEstimateDto(
            PolicyCode: scenario.Policy.Code,
            IsTaxed: scenario.Policy.IsTaxed,
            GainInTaxCurrency: impact.Total.GainInTaxCurrency,
            TaxChange: impact.Total.TaxChange,
            FxRate: scenario.Rate,
            Sales: [.. sales.Select(sale => EstimateOf(sale, impact.PerSale[sale.Key]))],
            Notes: notes,
            YearTaxBefore: impact.YearTaxBefore,
            CarriedLoss: impact.Total.CarriedLoss,
            Year: situation.Year,
            TaxCurrency: scenario.Policy.FxRule.TaxCurrency);
    }

    private static SaleEstimateDto EstimateOf(ProposedSaleDto sale, TaxImpact impact) =>
        new(sale.Key, impact.GainInTaxCurrency, impact.TaxChange, impact.CarriedLoss);

    private sealed record SaleScenario(
        Guid AccountId,
        DateOnly AsOf,
        IReadOnlyList<TaxEvent> Events,
        IReadOnlyList<LossYear> Losses,
        ITaxPolicy Policy,
        decimal Rate,
        DateOnly? RateDate)
    {
        public TaxEvent EventOf(ProposedSaleDto sale, int index)
        {
            var shareOfSub = sale.Quantity / Math.Max(1, sale.SubPortfolioIds.Count);
            var allocations = sale.SubPortfolioIds.Select(id => new EntryAllocation(id, shareOfSub));

            return new TaxEvent(
                sale.Key,
                AsOf,
                Events.Count + index,
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
