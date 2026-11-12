using InvestmentLab.SharedKernel;
using InvestmentLab.StrategyBuilder.Funds.Domain;

namespace InvestmentLab.StrategyBuilder.Funds.Application.SearchFunds;

public sealed record SearchFundsCommand(
    string? ExchangeId,
    string? RegionId,
    string? FundFamily,
    decimal? MinAssets,
    decimal? MaxExpenseRatio,
    IReadOnlyList<string>? NameWords);

public sealed record FoundFundDto(
    string Symbol, string Name, string? Currency, string? FundFamily, decimal? TotalAssets, decimal? ExpenseRatio, Guid? InstrumentId);

public sealed class SearchFundsHandler(IFundSearch search) : IHandler
{
    public async Task<Result<IReadOnlyList<FoundFundDto>>> Handle(SearchFundsCommand command, CancellationToken cancellationToken)
    {
        var criteria = FundCriteria.TryOf(
            exchangeId: command.ExchangeId,
            regionId: command.RegionId,
            fundFamily: command.FundFamily,
            minAssets: command.MinAssets,
            maxExpenseRatio: command.MaxExpenseRatio,
            nameWords: command.NameWords ?? []);
        if (criteria.IsFailure)
            return criteria.Error;

        var result = await search.SearchAsync(criteria.Value, cancellationToken);
        if (result.IsFailure)
            return result.Error;

        var funds = result.Value.Select(FundOf).ToList();

        return Result<IReadOnlyList<FoundFundDto>>.Success(funds);
    }

    private static FoundFundDto FundOf(FoundFund fund) => new(
        Symbol: fund.Symbol,
        Name: fund.Name,
        Currency: fund.Currency,
        FundFamily: fund.FundFamily,
        TotalAssets: fund.TotalAssets,
        ExpenseRatio: fund.ExpenseRatio,
        InstrumentId: fund.InstrumentId);
}
