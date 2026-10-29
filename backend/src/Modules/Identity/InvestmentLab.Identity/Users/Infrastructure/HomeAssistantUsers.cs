using InvestmentLab.Identity.Contracts;
using Microsoft.AspNetCore.Identity;

namespace InvestmentLab.Identity.Users.Infrastructure;

internal sealed class HomeAssistantUsers(UserManager<User> users) : IHomeAssistantUsers
{
    public async Task<string?> FindHomeAssistantUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        await users.FindByIdAsync(userId.ToString()) is { } user
            ? (await users.GetLoginsAsync(user)).FirstOrDefault(login => login.LoginProvider == HomeAssistantAuthenticationHandler.LoginProvider)?.ProviderKey
            : null;
}
