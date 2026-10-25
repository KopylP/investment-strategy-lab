namespace InvestmentLab.Identity.Contracts;

/// <summary>A new user signed up; other modules may prepare default data for them.</summary>
public sealed record UserRegisteredV1(Guid UserId, string Email, DateTimeOffset RegisteredAt);
