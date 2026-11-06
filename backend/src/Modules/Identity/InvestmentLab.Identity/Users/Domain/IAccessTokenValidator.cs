namespace InvestmentLab.Identity.Users.Domain;

public interface IAccessTokenValidator
{
    Task<ExternalSignIn?> ValidateAsync(string accessToken, CancellationToken cancellationToken);
}
