using InvestmentLab.Rebalancing.Reports.Application.Shared;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.ApplyJournalMatches;

public sealed class ApplyJournalMatchesHandler(JournalOperationsLoader loader, IReportRepository reports, TimeProvider clock) : IHandler
{
    public async Task<Result<int>> Handle(Guid reportId, CancellationToken cancellationToken)
    {
        var loaded = await loader.LoadAsync(reportId, cancellationToken);
        if (loaded.IsFailure)
            return loaded.Error;

        var report = loaded.Value.Report;
        var matches = JournalMatcher.Match(report, loaded.Value.Operations);

        var linked = report.LinkJournalOperations(matches, clock.GetUtcNow());
        if (linked.IsFailure)
            return linked.Error;

        var saved = await reports.TrySaveChangesAsync(cancellationToken);
        if (!saved)
            return ReportErrors.ChangedMeanwhile(reportId);

        return matches.Count;
    }
}
