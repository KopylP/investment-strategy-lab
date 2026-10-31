using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Identity.Profiles.Application.GetProfile;
using InvestmentLab.Identity.Profiles.Application.UpdateProfile;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Identity.Profiles.Api;

internal static class ProfileEndpoints
{
    public static RouteGroupBuilder MapProfiles(this RouteGroupBuilder group)
    {
        var profile = group.MapGroup("/profile").WithTags("Profile");

        profile.MapGet("", GetProfile).WithName("GetProfile");
        profile.MapPut("", UpdateProfile).WithName("UpdateProfile");

        return group;
    }

    private static async Task<IResult> GetProfile(GetProfileHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var profile = await handler.Handle(user.RequiredId, cancellationToken);

        return profile.ToHttpResult();
    }

    private static async Task<IResult> UpdateProfile(
        UpdateProfileCommand body, UpdateProfileHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var updated = await handler.Handle(user.RequiredId, body, cancellationToken);

        return updated.ToHttpResult();
    }
}
