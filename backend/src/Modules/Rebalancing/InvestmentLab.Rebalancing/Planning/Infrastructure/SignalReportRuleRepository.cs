using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class SignalReportRuleRepository(RebalancingDbContext db) : ISignalReportRuleRepository
{
    public Task<SignalReportRule?> FindAsync(Guid ruleId, CancellationToken cancellationToken) =>
        db.SignalReportRules.SingleOrDefaultAsync(rule => rule.Id == ruleId, cancellationToken);

    public async Task<IReadOnlyList<SignalReportRule>> AllAsync(CancellationToken cancellationToken) =>
        await db.SignalReportRules
            .Where(rule => rule.DismissedAt == null)
            .OrderBy(rule => rule.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SignalReportRule>> AllWithDismissedAsync(CancellationToken cancellationToken) =>
        await db.SignalReportRules.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SignalReportRule>> OfOwnedSignalAsync(Guid signalId, Guid ownerId, CancellationToken cancellationToken) =>
        await OfOwnedSignal(signalId, ownerId)
            .Where(rule => rule.DismissedAt == null)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SignalReportRule>> OfOwnedSignalWithDismissedAsync(Guid signalId, Guid ownerId, CancellationToken cancellationToken) =>
        await OfOwnedSignal(signalId, ownerId).ToListAsync(cancellationToken);

    private IQueryable<SignalReportRule> OfOwnedSignal(Guid signalId, Guid ownerId)
    {
        var owner = UserId.Of(ownerId);

        return db.SignalReportRules
            .Where(rule => rule.SignalId == signalId)
            .Where(rule => rule.OwnerId == owner);
    }

    public Task<SignalReportRule?> FindAsync(Guid signalId, Guid accountId, string reportCode, CancellationToken cancellationToken) =>
        db.SignalReportRules
            .Where(rule => rule.SignalId == signalId)
            .Where(rule => rule.AccountId == accountId)
            .SingleOrDefaultAsync(rule => rule.ReportCode == reportCode, cancellationToken);

    public void Add(SignalReportRule rule) => db.SignalReportRules.Add(rule);

    public void Remove(SignalReportRule rule) => db.SignalReportRules.Remove(rule);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
