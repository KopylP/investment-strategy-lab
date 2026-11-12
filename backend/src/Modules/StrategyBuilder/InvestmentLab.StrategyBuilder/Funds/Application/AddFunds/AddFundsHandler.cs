using InvestmentLab.StrategyBuilder.Funds.Domain;

namespace InvestmentLab.StrategyBuilder.Funds.Application.AddFunds;

public sealed record FundToAddDto(string Symbol, string Name, string? Currency, Guid? InstrumentId);

public sealed record AddFundsCommand(IReadOnlyList<FundToAddDto>? Funds, IReadOnlyList<Guid>? SelectedInstrumentIds);

public sealed record FundNotAddedDto(string Symbol, string Reason);

public sealed record AddedFundsDto(IReadOnlyList<Guid> InstrumentIds, IReadOnlyList<FundNotAddedDto> Failed);

public sealed class AddFundsHandler(IFundSearch search) : IHandler
{
    public async Task<Result<AddedFundsDto>> Handle(AddFundsCommand command, CancellationToken cancellationToken)
    {
        var funds = (command.Funds ?? []).Select(FundOf).ToList();
        var addition = FundAddition.TryOf(funds, command.SelectedInstrumentIds ?? []);
        if (addition.IsFailure)
            return addition.Error;

        var added = await search.AddAsync(addition.Value.Funds, cancellationToken);
        var failed = added.Failed.Select(fund => new FundNotAddedDto(fund.Symbol, fund.Reason)).ToList();

        return new AddedFundsDto(added.InstrumentIds, failed);
    }

    private static FoundFund FundOf(FundToAddDto fund) => new(
        Symbol: fund.Symbol ?? "",
        Name: fund.Name ?? "",
        Currency: fund.Currency,
        FundFamily: null,
        TotalAssets: null,
        ExpenseRatio: null,
        InstrumentId: fund.InstrumentId);
}
