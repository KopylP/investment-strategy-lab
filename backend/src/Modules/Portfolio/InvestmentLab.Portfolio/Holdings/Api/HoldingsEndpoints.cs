using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Portfolio.Holdings.Application.GetHoldings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Portfolio.Holdings.Api;

internal static class HoldingsEndpoints
{
    public static RouteGroupBuilder MapHoldings(this RouteGroupBuilder group)
    {
        group.MapGet("/accounts/{accountId:guid}/holdings", async (Guid accountId, DateOnly? asOf, GetHoldingsHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, asOf, ct)).ToHttpResult())
            .WithTags("Holdings")
            .WithName("GetHoldings");

        return group;
    }
}
