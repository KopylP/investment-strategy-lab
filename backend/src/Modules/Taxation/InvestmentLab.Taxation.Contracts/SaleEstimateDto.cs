namespace InvestmentLab.Taxation.Contracts;

public sealed record SaleEstimateDto(Guid Key, decimal GainInTaxCurrency, decimal TaxChange, decimal CarriedLoss = 0m);
