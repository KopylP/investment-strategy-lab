namespace InvestmentLab.StrategyBuilder.Funds.Domain;

public interface IFundSearch
{
    FundSearchOptions Options();

    Task<Result<IReadOnlyList<FoundFund>>> SearchAsync(FundCriteria criteria, CancellationToken cancellationToken);

    Task<AddedFunds> AddAsync(IReadOnlyList<FoundFund> funds, CancellationToken cancellationToken);
}
