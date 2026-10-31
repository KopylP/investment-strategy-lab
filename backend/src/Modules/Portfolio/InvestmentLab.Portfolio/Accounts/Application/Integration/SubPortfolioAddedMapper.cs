using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application.Integration;

internal sealed class SubPortfolioAddedMapper : IDomainEventMapper<SubPortfolioAdded, SubPortfolioAddedV1>
{
    public SubPortfolioAddedV1 Map(SubPortfolioAdded added) =>
        new(added.AccountId.Value, added.SubPortfolioId.Value, added.Name);
}
