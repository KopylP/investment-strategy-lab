using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Integration;

internal sealed class MarkUndoneMapper : IDomainEventMapper<MarkUndone, ReverseTransactionV1>
{
    private const string Reason = "відмітку в рапорті скасовано";

    public ReverseTransactionV1 Map(MarkUndone undone) =>
        new(undone.AccountId, undone.Reference, Reason);
}
