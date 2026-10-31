using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Portfolio.Accounts.Application.Integration;

internal sealed class TargetSharesChangedMapper : IDomainEventMapper<TargetSharesChanged, TargetSharesChangedV1>
{
    public TargetSharesChangedV1 Map(TargetSharesChanged changed) =>
        new(changed.AccountId.Value, changed.TargetShares, changed.OccurredAt);
}
