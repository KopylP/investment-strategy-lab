using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.StrategyBuilder.Funds.Application.AddFunds;
using InvestmentLab.StrategyBuilder.Funds.Application.GetFundSearchOptions;
using InvestmentLab.StrategyBuilder.Funds.Application.SearchFunds;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.StrategyBuilder.Funds.Api;

internal static class FundEndpoints
{
    public static RouteGroupBuilder MapFunds(this RouteGroupBuilder group)
    {
        var funds = group
            .MapGroup("/funds")
            .WithTags("StrategyBuilder");

        funds
            .MapGet("/options", GetOptions)
            .WithName("GetBuilderFundSearchOptions")
            .Produces<FundSearchOptionsDto>();
        funds
            .MapPost("/search", Search)
            .WithName("SearchBuilderFunds")
            .Produces<IReadOnlyList<FoundFundDto>>();
        funds
            .MapPost(string.Empty, Add)
            .WithName("AddBuilderFunds")
            .Produces<AddedFundsDto>();

        return group;
    }

    private static IResult GetOptions(GetFundSearchOptionsHandler handler) => TypedResults.Ok(handler.Handle());

    private static async Task<IResult> Search(SearchFundsCommand body, SearchFundsHandler handler, CancellationToken cancellationToken)
    {
        var found = await handler.Handle(body, cancellationToken);

        return found.ToHttpResult();
    }

    private static async Task<IResult> Add(AddFundsCommand body, AddFundsHandler handler, CancellationToken cancellationToken)
    {
        var added = await handler.Handle(body, cancellationToken);

        return added.ToHttpResult();
    }
}
