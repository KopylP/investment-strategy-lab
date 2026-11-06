namespace InvestmentLab.Identity.Users.Infrastructure;

internal sealed class AccessTokenNotLinkedException() : Exception("The token's user has no linked account.");
