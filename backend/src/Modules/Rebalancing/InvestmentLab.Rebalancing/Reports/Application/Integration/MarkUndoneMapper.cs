using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Integration;

internal sealed class MarkUndoneMapper : IDomainEventMapper<MarkUndone, ReverseTransactionV1>
{
    private static readonly string Reason = Message.Of("report.action.mark_undone");

    public ReverseTransactionV1 Map(MarkUndone undone) =>
        new(undone.AccountId, undone.Reference, Reason);
}
