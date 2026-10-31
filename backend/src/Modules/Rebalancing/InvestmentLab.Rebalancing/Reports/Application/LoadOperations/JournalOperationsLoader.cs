using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.LoadOperations;

public sealed class JournalOperationsLoader(IReportRepository reports, IJournalQueries journal)
{
    public async Task<Result<LoadedOperations>> LoadAsync(Guid reportId, CancellationToken cancellationToken)
    {
        if (reportId == Guid.Empty || await reports.FindAsync(ReportId.Of(reportId), cancellationToken) is not { } report)
            return ReportErrors.NotFound(reportId);

        if (!report.IsOpen)
            return ReportErrors.ReportIsNotOpen(report.Id.Value, report.Status);

        var snapshot = await journal.GetJournal(report.AccountId, cancellationToken);
        if (snapshot is null)
            return ReportErrors.NotFound(reportId);

        var taken = await TransactionsLinkedToReportsAsync(report.AccountId, cancellationToken);
        var free = EntriesFreeToLink(snapshot, taken);

        var operations = free.Select(ToOperation).OfType<JournalOperation>().ToList();
        var sources = free.ToDictionary(entry => entry.TransactionId, entry => entry.Source);

        return new LoadedOperations(report, operations, sources);
    }

    private async Task<HashSet<Guid>> TransactionsLinkedToReportsAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var accountReports = await reports.ListAsync(accountId, cancellationToken);

        return [.. accountReports
            .SelectMany(report => report.Actions)
            .SelectMany(action => action.Fills)
            .Select(fill => fill.TransactionId)
            .OfType<Guid>()];
    }

    private static List<TransactionRecordedV1> EntriesFreeToLink(JournalSnapshotDto snapshot, HashSet<Guid> taken)
    {
        var reversed = snapshot.ReversedIds.ToHashSet();

        return [.. snapshot.Entries
            .Where(entry => entry.Source is "Import" or "Manual")
            .Where(entry => !reversed.Contains(entry.TransactionId))
            .Where(entry => !taken.Contains(entry.TransactionId))];
    }

    private static JournalOperation? ToOperation(TransactionRecordedV1 entry) => entry switch
    {
        { Trade: { } trade } => FromTrade(entry, trade),
        { Cash: { Kind: "Contribution" } cash } => new JournalOperation(entry.TransactionId, entry.OccurredOn, JournalOperationKind.Contribution, null, cash.Amount, 1m, 0m),
        _ => null,
    };

    private static JournalOperation FromTrade(TransactionRecordedV1 entry, TradeRecordedDto trade)
    {
        var kind = trade.Side == "Sell" ? JournalOperationKind.Sell : JournalOperationKind.Buy;

        return new JournalOperation(entry.TransactionId, entry.OccurredOn, kind, trade.InstrumentId, trade.Quantity, trade.Price, trade.Fee);
    }
}
