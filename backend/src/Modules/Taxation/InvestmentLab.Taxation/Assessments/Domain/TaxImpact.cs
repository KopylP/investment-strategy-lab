namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record TaxImpact(decimal GainInTaxCurrency, decimal TaxChange, decimal CarriedLoss);
