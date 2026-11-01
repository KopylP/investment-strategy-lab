namespace InvestmentLab.Taxation.Contracts;

public sealed record TaxEstimateDto(
    string PolicyCode,
    bool IsTaxed,
    decimal GainInTaxCurrency,
    decimal TaxChange,
    decimal? FxRate,
    IReadOnlyList<SaleEstimateDto> Sales,
    IReadOnlyList<string> Notes,
    decimal? YearTaxBefore = null,
    decimal CarriedLoss = 0m,
    int? Year = null,
    string? TaxCurrency = null);
