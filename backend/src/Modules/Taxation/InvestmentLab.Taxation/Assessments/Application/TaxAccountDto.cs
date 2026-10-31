namespace InvestmentLab.Taxation.Assessments.Application;

public sealed record TaxAccountDto(Guid Id, string Name, string Broker, string Currency, string PolicyCode, bool IsTaxed);
