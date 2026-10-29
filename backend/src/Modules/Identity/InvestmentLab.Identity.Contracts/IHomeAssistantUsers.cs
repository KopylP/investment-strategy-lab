namespace InvestmentLab.Identity.Contracts;

/// <summary>The Home Assistant user an account is linked to: whoever opened the add-on's panel with it.</summary>
public interface IHomeAssistantUsers
{
    /// <returns>Null when the account was never opened through the panel.</returns>
    Task<string?> FindHomeAssistantUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
