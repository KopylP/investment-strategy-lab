using InvestmentLab.SharedKernel;
using InvestmentLab.Identity.Profiles.Application.Shared;
using InvestmentLab.Identity.Profiles.Domain;
using InvestmentLab.Identity.Users.Domain;

namespace InvestmentLab.Identity.Profiles.Application.GetProfile;

public sealed class GetProfileHandler(IUserProfileRepository profiles) : IHandler
{
    public async Task<Result<UserProfileDto>> Handle(UserId userId, CancellationToken cancellationToken) =>
        await profiles.FindAsync(userId, cancellationToken) is { } profile
            ? profile.ToDto()
            : ProfileErrors.NotFound(userId);
}
