namespace InvestmentLab.Taxation.Contracts;

public sealed record SimulatedTaxDto(string PolicyCode, string TaxCurrency, IReadOnlyList<SimulatedTaxYearDto> Years);
