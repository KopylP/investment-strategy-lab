using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Taxation.Contracts;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Rebalancing.Planning.Application;

public sealed class SaleTaxes(ITaxEstimator estimator, ILogger<SaleTaxes> logger)
{
    public async Task EstimateAsync(RebalancingReport report, DateOnly asOf, CancellationToken cancellationToken)
    {
        var sales = report.Sales.Where(action => action.InstrumentId is not null).ToList();
        if (sales.Count == 0)
            return;

        try
        {
            var estimate = await estimator.EstimateSalesAsync(report.AccountId, [.. sales.Select(action => new ProposedSaleDto(
                action.Id.Value, action.InstrumentId!.Value, action.PlannedQuantity, action.PlannedPrice,
                [.. action.Allocations.Select(allocation => allocation.SubPortfolioId)]))], asOf, cancellationToken);

            report.AttachTaxEstimate(
                new TaxEstimate(estimate.PolicyCode, estimate.IsTaxed, estimate.GainPln, estimate.TaxPln, estimate.FxRate, estimate.Notes),
                estimate.Sales.ToDictionary(sale => ReportActionId.Of(sale.Key), sale => (sale.GainPln, sale.TaxPln)));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Tax of report {ReportId} was not estimated", report.Id);
        }
    }
}
