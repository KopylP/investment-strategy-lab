using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Identity.Users.Application.Shared;
using InvestmentLab.Identity.Users.Domain;

namespace InvestmentLab.Identity.Users.Application.GetCurrentUser;

public sealed class GetCurrentUserHandler(ICurrentUser user, IUserAccounts accounts)
{
    public async Task<Result<CurrentUserDto>> Handle(bool viaHomeAssistant, CancellationToken cancellationToken)
    {
        if (await accounts.FindAsync(user.RequiredId, cancellationToken) is not { } me)
            return UserErrors.NotFound(user.RequiredId);

        return me with { ViaHomeAssistant = viaHomeAssistant };
    }
}
