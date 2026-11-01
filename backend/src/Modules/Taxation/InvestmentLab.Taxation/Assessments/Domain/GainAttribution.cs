namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record GainAttribution(Guid SubPortfolioId, decimal GainInTaxCurrency);
