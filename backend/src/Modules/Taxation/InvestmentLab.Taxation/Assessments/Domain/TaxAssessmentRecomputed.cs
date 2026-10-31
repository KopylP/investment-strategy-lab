namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record TaxAssessmentRecomputed(UserId OwnerId, int Year, decimal TaxDue, DateTimeOffset OccurredAt) : IDomainEvent;
