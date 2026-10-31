using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Integration;

internal sealed class RollBackRejectedTransaction(IReportRepository reports) : IIntegrationEventHandler<TransactionRejectedV1>
{
    public async Task Handle(TransactionRejectedV1 rejected, CancellationToken cancellationToken)
    {
        if (FillReference.TryParse(rejected.SourceReference) is not { } reference)
            return;

        if (await reports.FindAsync(reference.ReportId, cancellationToken) is not { } report)
            return;

        report.ApplyTransactionRejected(reference, rejected.Reason);

        await reports.SaveChangesAsync(cancellationToken);
    }
}
