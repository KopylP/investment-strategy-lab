namespace InvestmentLab.Taxation.Contracts;

public sealed record SaleEstimateDto(Guid Key, decimal GainPln, decimal TaxPln, decimal CarriedLossPln = 0m);
