namespace InvestmentLab.Identity.Contracts;

public sealed record UserRegisteredV1(Guid UserId, string Email, DateTimeOffset RegisteredAt);
