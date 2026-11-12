using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record TaxYearSummaryDto(
    int Year,
    decimal Revenue,
    decimal Costs,
    decimal Gain,
    decimal CarryforwardUsed,
    decimal TaxBase,
    decimal CapitalTax,
    decimal DividendIncome,
    decimal DividendTax,
    decimal TaxDue,
    decimal LossToCarry,
    decimal DividendTaxCredit)
{
    public static TaxYearSummaryDto From(TaxYearSummary taxYearSummary) => new(
        Year: taxYearSummary.Year,
        Revenue: taxYearSummary.Revenue,
        Costs: taxYearSummary.Costs,
        Gain: taxYearSummary.Gain,
        CarryforwardUsed: taxYearSummary.CarryforwardUsed,
        TaxBase: taxYearSummary.TaxBase,
        CapitalTax: taxYearSummary.CapitalTax,
        DividendIncome: taxYearSummary.DividendIncome,
        DividendTax: taxYearSummary.DividendTax,
        TaxDue: taxYearSummary.TaxDue,
        LossToCarry: taxYearSummary.LossToCarry,
        DividendTaxCredit: taxYearSummary.DividendTaxCredit);
}
