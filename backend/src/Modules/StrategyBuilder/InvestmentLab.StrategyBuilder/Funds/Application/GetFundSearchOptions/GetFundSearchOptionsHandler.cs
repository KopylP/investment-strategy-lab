using InvestmentLab.SharedKernel;
using InvestmentLab.StrategyBuilder.Funds.Domain;

namespace InvestmentLab.StrategyBuilder.Funds.Application.GetFundSearchOptions;

public sealed record FundSearchChoiceDto(string Id, string Label);

public sealed record FundSearchOptionsDto(IReadOnlyList<FundSearchChoiceDto> Exchanges, IReadOnlyList<FundSearchChoiceDto> Regions);

public sealed class GetFundSearchOptionsHandler(IFundSearch search) : IHandler
{
    public FundSearchOptionsDto Handle()
    {
        var options = search.Options();
        var exchanges = options.Exchanges.Select(ChoiceOf).ToList();
        var regions = options.Regions.Select(ChoiceOf).ToList();

        return new FundSearchOptionsDto(exchanges, regions);
    }

    private static FundSearchChoiceDto ChoiceOf(FundSearchChoice choice) => new(choice.Id, choice.Label);
}
