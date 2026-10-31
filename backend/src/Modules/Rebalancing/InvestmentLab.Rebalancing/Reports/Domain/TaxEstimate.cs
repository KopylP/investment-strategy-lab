namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record TaxEstimate(string PolicyCode, bool IsTaxed, decimal GainPln, decimal TaxPln, decimal? FxRate, IReadOnlyList<string> Notes);
