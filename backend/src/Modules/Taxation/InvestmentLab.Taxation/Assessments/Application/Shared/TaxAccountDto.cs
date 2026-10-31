namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record TaxAccountDto(Guid Id, string Name, string Broker, string Currency, string PolicyCode, bool IsTaxed);
