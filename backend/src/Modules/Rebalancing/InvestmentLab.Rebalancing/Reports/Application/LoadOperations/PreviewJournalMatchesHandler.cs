using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.LoadOperations;

public sealed class PreviewJournalMatchesHandler(JournalOperationsLoader loader)
{
    public async Task<Result<LoadOperationsPreviewDto>> Handle(Guid reportId, CancellationToken cancellationToken)
    {
        var loaded = await loader.LoadAsync(reportId, cancellationToken);
        if (loaded.IsFailure)
            return loaded.Error;

        var (report, operations, sources) = loaded.Value;
        var matches = JournalMatcher.Match(report, operations);

        var matched = matches.Select(match => ToMatchDto(report, match, sources)).ToList();
        var unmatched = UnmatchedAfterReport(report, operations, matches)
            .Select(operation => ToOperationDto(operation, sources))
            .ToList();

        return new LoadOperationsPreviewDto(matched, unmatched);
    }

    private static IEnumerable<JournalOperation> UnmatchedAfterReport(
        RebalancingReport report, IReadOnlyList<JournalOperation> operations, IReadOnlyList<ActionMatch> matches)
    {
        var used = matches
            .SelectMany(match => match.Operations)
            .Select(operation => operation.TransactionId)
            .ToHashSet();
        var generatedOn = DateOnly.FromDateTime(report.GeneratedAt.UtcDateTime);

        return operations
            .Where(operation => operation.OccurredOn >= generatedOn)
            .Where(operation => !used.Contains(operation.TransactionId));
    }

    private static ActionMatchDto ToMatchDto(RebalancingReport report, ActionMatch match, IReadOnlyDictionary<Guid, string> sources)
    {
        var action = report.FindAction(match.ActionId)!;
        var operations = match.Operations.Select(operation => ToOperationDto(operation, sources)).ToList();

        return new ActionMatchDto(
            action.Id.Value,
            action.Sequence,
            action.Kind.ToString(),
            action.InstrumentId,
            action.PlannedQuantity,
            action.RemainingQuantity,
            match.Completes,
            operations);
    }

    private static MatchedOperationDto ToOperationDto(JournalOperation operation, IReadOnlyDictionary<Guid, string> sources) => new(
        operation.TransactionId,
        operation.OccurredOn,
        operation.Kind.ToString(),
        operation.InstrumentId,
        operation.Quantity,
        operation.Price,
        operation.Fee,
        sources.GetValueOrDefault(operation.TransactionId, ""));
}
