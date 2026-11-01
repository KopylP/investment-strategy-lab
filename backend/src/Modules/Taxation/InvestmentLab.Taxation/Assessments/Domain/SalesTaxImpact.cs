namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record SalesTaxImpact(decimal YearTaxBeforePln, TaxImpact Total, IReadOnlyDictionary<Guid, TaxImpact> PerSale);
