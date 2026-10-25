using System.Security.Claims;
using InvestmentLab.SharedKernel;
using Microsoft.AspNetCore.Http;

namespace InvestmentLab.BuildingBlocks.Security;

/// <summary>Reads the user id from the authentication cookie's name identifier claim.</summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public UserId? Id =>
        Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id != Guid.Empty
            ? UserId.Of(id)
            : null;
}
