using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Portfolio.Holdings.Application.GetHoldings;
using InvestmentLab.Portfolio.Contracts.Holdings;
using InvestmentLab.Portfolio.Holdings.Application.GetValuedHoldings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Portfolio.Holdings.Api;

internal static class HoldingsEndpoints
{
    public static RouteGroupBuilder MapHoldings(this RouteGroupBuilder group)
    {
        group
            .MapGet("/accounts/{accountId:guid}/holdings", GetHoldings)
            .WithTags("Holdings")
            .WithName("GetHoldings")
            .Produces<AccountHoldingsDto>();
        group
            .MapGet("/accounts/{accountId:guid}/holdings/valued", GetValuedHoldings)
            .WithTags("Holdings")
            .WithName("GetValuedHoldings")
            .Produces<ValuedHoldingsDto>();

        return group;
    }

    private static async Task<IResult> GetHoldings(
        Guid accountId,
        DateOnly? asOf,
        GetHoldingsHandler handler,
        CancellationToken cancellationToken)
    {
        var holdings = await handler.Handle(accountId, asOf, cancellationToken);

        return holdings.ToHttpResult();
    }

    private static async Task<IResult> GetValuedHoldings(
        Guid accountId,
        DateOnly? asOf,
        GetValuedHoldingsHandler handler,
        CancellationToken cancellationToken)
    {
        var holdings = await handler.Handle(accountId, asOf, cancellationToken);

        return holdings.ToHttpResult();
    }
}
