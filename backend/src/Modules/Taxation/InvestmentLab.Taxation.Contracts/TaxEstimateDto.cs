namespace InvestmentLab.Taxation.Contracts;

public sealed record TaxEstimateDto(
    string PolicyCode,
    bool IsTaxed,
    decimal GainPln,
    decimal TaxPln,
    decimal? FxRate,
    IReadOnlyList<SaleEstimateDto> Sales,
    IReadOnlyList<string> Notes);
