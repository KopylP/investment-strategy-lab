namespace InvestmentLab.Identity.Contracts;

public interface IHomeAssistantUsers
{
    Task<string?> FindHomeAssistantUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
