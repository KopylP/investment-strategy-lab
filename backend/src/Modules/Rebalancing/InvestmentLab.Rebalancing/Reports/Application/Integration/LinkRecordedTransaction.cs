using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Integration;

internal sealed class LinkRecordedTransaction(IReportRepository reports) : IIntegrationEventHandler<TransactionRecordedV1>
{
    public async Task Handle(TransactionRecordedV1 recorded, CancellationToken cancellationToken)
    {
        if (FillReference.TryParse(recorded.SourceReference) is not { } reference)
            return;

        if (await reports.FindAsync(reference.ReportId, cancellationToken) is not { } report)
            return;

        report.ApplyTransactionRecorded(reference, recorded.TransactionId);

        await reports.SaveChangesAsync(cancellationToken);
    }
}
