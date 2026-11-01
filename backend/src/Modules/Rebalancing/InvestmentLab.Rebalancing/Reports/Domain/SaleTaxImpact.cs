namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record SaleTaxImpact(decimal GainPln, decimal TaxChangePln, decimal CarriedLossPln);
