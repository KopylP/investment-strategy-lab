namespace InvestmentLab.Taxation.Contracts;

/// <summary>A taxpayer's tax of a year changed (new trades or reversals in any taxed account, rates, carried losses).</summary>
public sealed record TaxAssessmentUpdatedV2(Guid OwnerId, int Year, decimal TaxDuePln, DateTimeOffset ComputedAt);

/// <summary>What selling now would cost in tax, for plans (Rebalancing). An estimate, never final.</summary>
public interface ITaxEstimator
{
    Task<TaxEstimateDto> EstimateSalesAsync(Guid accountId, IReadOnlyList<ProposedSaleDto> sales, DateOnly asOf, CancellationToken cancellationToken);
}

/// <param name="Price">In the account currency.</param>
public sealed record ProposedSaleDto(Guid Key, Guid InstrumentId, decimal Quantity, decimal Price, IReadOnlyList<Guid> SubPortfolioIds);

/// <param name="TaxPln">
/// Extra tax of the year if all sales happen (marginal: gains and losses of the year so far in all the owner's taxed
/// accounts count, as in one PIT-38).
/// </param>
public sealed record TaxEstimateDto(
    string PolicyCode,
    bool IsTaxed,
    decimal GainPln,
    decimal TaxPln,
    decimal? FxRate,
    IReadOnlyList<SaleEstimateDto> Sales,
    IReadOnlyList<string> Notes);

/// <param name="Key">The key of the proposed sale.</param>
public sealed record SaleEstimateDto(Guid Key, decimal GainPln, decimal TaxPln);
