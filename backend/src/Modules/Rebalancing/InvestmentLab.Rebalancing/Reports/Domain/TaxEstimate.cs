namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record TaxEstimate(
    string PolicyCode,
    bool IsTaxed,
    decimal GainInTaxCurrency,
    decimal TaxChange,
    decimal? FxRate,
    IReadOnlyList<string> Notes,
    decimal? YearTaxBefore = null,
    decimal CarriedLoss = 0m,
    int? Year = null,
    string? TaxCurrency = null);
