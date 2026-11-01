using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Taxation.Contracts;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

public sealed class SaleTaxes(ITaxEstimator estimator, ILogger<SaleTaxes> logger)
{
    public async Task EstimateAsync(RebalancingReport report, DateOnly asOf, CancellationToken cancellationToken)
    {
        var sales = report.Sales.Where(action => action.InstrumentId is not null).ToList();
        if (sales.Count == 0)
            return;

        try
        {
            var proposed = sales.Select(ProposedSaleOf).ToList();
            var estimate = await estimator.EstimateSalesAsync(report.AccountId, proposed, asOf, cancellationToken);

            report.AttachTaxEstimate(ReportEstimateOf(estimate), ImpactPerSale(estimate));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Tax of report {ReportId} was not estimated", report.Id);
        }
    }

    private static ProposedSaleDto ProposedSaleOf(ReportAction action)
    {
        var subPortfolios = action.Allocations.Select(allocation => allocation.SubPortfolioId).ToList();

        return new ProposedSaleDto(action.Id.Value, action.InstrumentId!.Value, action.PlannedQuantity, action.PlannedPrice, subPortfolios);
    }

    private static TaxEstimate ReportEstimateOf(TaxEstimateDto estimate) => new(
        PolicyCode: estimate.PolicyCode,
        IsTaxed: estimate.IsTaxed,
        GainInTaxCurrency: estimate.GainInTaxCurrency,
        TaxChange: estimate.TaxChange,
        FxRate: estimate.FxRate,
        Notes: estimate.Notes,
        YearTaxBefore: estimate.YearTaxBefore,
        CarriedLoss: estimate.CarriedLoss,
        Year: estimate.Year,
        TaxCurrency: estimate.TaxCurrency);

    private static Dictionary<ReportActionId, SaleTaxImpact> ImpactPerSale(TaxEstimateDto estimate) =>
        estimate.Sales.ToDictionary(
            sale => ReportActionId.Of(sale.Key),
            sale => new SaleTaxImpact(sale.GainInTaxCurrency, sale.TaxChange, sale.CarriedLoss));
}
