namespace InvestmentLab.Identity.Contracts;

public interface IUserLanguages
{
    Task<string?> FindChosenLanguageAsync(Guid userId, CancellationToken cancellationToken);
}
