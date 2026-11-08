using InvestmentLab.Identity.Contracts;
using InvestmentLab.Identity.Profiles.Domain;

namespace InvestmentLab.Identity.Profiles.Infrastructure;

internal sealed class UserLanguages(IUserProfileRepository profiles) : IUserLanguages
{
    public async Task<string?> FindChosenLanguageAsync(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await profiles.FindAsync(UserId.Of(userId), cancellationToken);

        return profile?.Language?.Code;
    }
}
