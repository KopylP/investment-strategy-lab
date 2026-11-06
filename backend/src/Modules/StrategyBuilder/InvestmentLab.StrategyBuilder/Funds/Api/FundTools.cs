using System.ComponentModel;
using InvestmentLab.BuildingBlocks.Mcp;
using InvestmentLab.StrategyBuilder.Funds.Application.AddFunds;
using InvestmentLab.StrategyBuilder.Funds.Application.GetFundSearchOptions;
using InvestmentLab.StrategyBuilder.Funds.Application.SearchFunds;
using ModelContextProtocol.Server;

namespace InvestmentLab.StrategyBuilder.Funds.Api;

[McpServerToolType]
internal static class FundTools
{
    [McpServerTool(Name = "get_fund_search_options", ReadOnly = true)]
    [Description("Exchanges and regions search_funds can filter by.")]
    public static FundSearchOptionsDto GetFundSearchOptions(GetFundSearchOptionsHandler handler) => handler.Handle();

    [McpServerTool(Name = "search_funds", ReadOnly = true, OpenWorld = true)]
    [Description("Searches listed funds (ETFs) by exchange, region, fund family, size, cost and words of the name.")]
    public static async Task<IReadOnlyList<FoundFundDto>> SearchFunds(
        SearchFundsCommand search,
        SearchFundsHandler handler,
        CancellationToken cancellationToken)
    {
        var found = await handler.Handle(search, cancellationToken);

        return found.ValueOrThrow();
    }

    [McpServerTool(Name = "add_funds")]
    [Description("Adds found funds to the user's instruments so a draft can use them; returns their instrument ids. Put them in a draft with change_draft.")]
    public static async Task<AddedFundsDto> AddFunds(AddFundsCommand funds, AddFundsHandler handler, CancellationToken cancellationToken)
    {
        var added = await handler.Handle(funds, cancellationToken);

        return added.ValueOrThrow();
    }
}
