namespace InvestmentLab.Identity.Profiles.Domain;

public sealed record UserRegistered(UserId UserId, string Email, DateTimeOffset OccurredAt) : IDomainEvent;
