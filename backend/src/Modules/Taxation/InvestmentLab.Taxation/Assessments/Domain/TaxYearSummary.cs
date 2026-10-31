namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record TaxYearSummary(
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
    decimal DividendTaxCredit = 0m);
