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

        profile.MapGet("", async (GetProfileHandler handler, ICurrentUser user, CancellationToken ct) =>
                (await handler.Handle(user.RequiredId, ct)).ToHttpResult())
            .WithName("GetProfile");

        profile.MapPut("", async (UpdateProfileCommand body, UpdateProfileHandler handler, ICurrentUser user, CancellationToken ct) =>
                (await handler.Handle(user.RequiredId, body, ct)).ToHttpResult())
            .WithName("UpdateProfile");

        return group;
    }
}
