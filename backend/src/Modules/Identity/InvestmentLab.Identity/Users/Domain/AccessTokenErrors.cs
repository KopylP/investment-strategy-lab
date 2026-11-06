namespace InvestmentLab.Identity.Users.Domain;

public static class AccessTokenErrors
{
    public static Error NotLinked() =>
        Error.Forbidden("identity.access_token.not_linked", "The token is valid, but its user has never signed in to the Investment Lab panel.");
}
