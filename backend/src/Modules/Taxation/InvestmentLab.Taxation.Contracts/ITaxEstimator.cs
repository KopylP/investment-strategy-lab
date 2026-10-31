namespace InvestmentLab.Taxation.Contracts;

public interface ITaxEstimator
{
    Task<TaxEstimateDto> EstimateSalesAsync(Guid accountId, IReadOnlyList<ProposedSaleDto> sales, DateOnly asOf, CancellationToken cancellationToken);
}
