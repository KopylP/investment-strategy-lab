using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Integration;

/// <summary>
/// Portfolio's answers to fills sent from reports: a recorded entry is linked to its fill, a rejected one takes the
/// mark back so the user sees why and can correct it. Both are idempotent.
/// </summary>
internal sealed class TransactionResults(IReportRepository reports)
    : IIntegrationEventHandler<TransactionRecordedV1>, IIntegrationEventHandler<TransactionRejectedV1>
{
    public async Task Handle(TransactionRecordedV1 recorded, CancellationToken cancellationToken)
    {
        if (FillReference.TryParse(recorded.SourceReference) is not { } reference
            || await reports.FindAsync(reference.ReportId, cancellationToken) is not { } report)
            return;

        report.ApplyTransactionRecorded(reference, recorded.TransactionId);
        await reports.SaveChangesAsync(cancellationToken);
    }

    public async Task Handle(TransactionRejectedV1 rejected, CancellationToken cancellationToken)
    {
        if (FillReference.TryParse(rejected.SourceReference) is not { } reference
            || await reports.FindAsync(reference.ReportId, cancellationToken) is not { } report)
            return;

        report.ApplyTransactionRejected(reference, rejected.Reason);
        await reports.SaveChangesAsync(cancellationToken);
    }
}
