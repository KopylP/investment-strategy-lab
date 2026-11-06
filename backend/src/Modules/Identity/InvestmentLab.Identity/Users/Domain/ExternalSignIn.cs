namespace InvestmentLab.Identity.Users.Domain;

public sealed record ExternalSignIn(string LoginProvider, string ExternalUserId);
