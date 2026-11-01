namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record SaleTaxImpact(decimal GainInTaxCurrency, decimal TaxChange, decimal CarriedLoss);
