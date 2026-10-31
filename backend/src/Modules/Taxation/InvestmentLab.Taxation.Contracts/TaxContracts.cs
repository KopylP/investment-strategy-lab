namespace InvestmentLab.Taxation.Contracts;

public sealed record TaxAssessmentUpdatedV2(Guid OwnerId, int Year, decimal TaxDuePln, DateTimeOffset ComputedAt);
