using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record TaxEstimateDto(
    string PolicyCode,
    bool IsTaxed,
    decimal GainInTaxCurrency,
    decimal TaxChange,
    decimal? FxRate,
    IReadOnlyList<string> Notes,
    decimal? YearTaxBefore,
    decimal CarriedLoss,
    int? Year,
    string? TaxCurrency)
{
    public static TaxEstimateDto From(TaxEstimate taxEstimate) => new(
        PolicyCode: taxEstimate.PolicyCode,
        IsTaxed: taxEstimate.IsTaxed,
        GainInTaxCurrency: taxEstimate.GainInTaxCurrency,
        TaxChange: taxEstimate.TaxChange,
        FxRate: taxEstimate.FxRate,
        Notes: taxEstimate.Notes,
        YearTaxBefore: taxEstimate.YearTaxBefore,
        CarriedLoss: taxEstimate.CarriedLoss,
        Year: taxEstimate.Year,
        TaxCurrency: taxEstimate.TaxCurrency);
}
