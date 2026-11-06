using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.StrategyBuilder.Drafts.Infrastructure;

internal sealed class DraftRepository(StrategyBuilderDbContext db) : IDraftRepository
{
    public async Task<StrategyDraft?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var draft = await db.Drafts.FirstOrDefaultAsync(draft => draft.Id == id, cancellationToken);
        draft?.NormalizeGraph();
        return draft;
    }

    public async Task<StrategyDraft?> FindByOriginAsync(Guid strategyId, CancellationToken cancellationToken)
    {
        var drafts = await db.Drafts.ToListAsync(cancellationToken);

        var draft = drafts.FirstOrDefault(draft => draft.Origin?.StrategyId == strategyId);
        draft?.NormalizeGraph();
        return draft;
    }

    public async Task<IReadOnlyList<StrategyDraft>> ListAsync(CancellationToken cancellationToken) =>
        await db.Drafts.OrderByDescending(draft => draft.UpdatedAt).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<StrategyDraft>> ReferencingSignalAsync(Guid signalId, CancellationToken cancellationToken)
    {
        var drafts = await db.Drafts.IgnoreQueryFilters().ToListAsync(cancellationToken);

        return [.. drafts.Where(draft => SignalsOf(draft).Contains(signalId))];
    }

    public void Add(StrategyDraft draft) => db.Drafts.Add(draft);

    public void Remove(StrategyDraft draft) => db.Drafts.Remove(draft);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    private static IEnumerable<Guid> SignalsOf(StrategyDraft draft)
    {
        var inRules = draft.Graph.Nodes.Select(node => node.Parameters.Text("signalId")).OfType<string>().Select(id => Guid.TryParse(id, out var parsed) ? parsed : Guid.Empty);
        var inReports = draft.Reports.SelectMany(report => report.SignalIds);

        return inRules.Concat(inReports);
    }
}
