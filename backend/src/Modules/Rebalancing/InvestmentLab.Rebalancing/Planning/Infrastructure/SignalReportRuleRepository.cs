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
            .OrderBy(rule => rule.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SignalReportRule>> OfOwnedSignalAsync(Guid signalId, Guid ownerId, CancellationToken cancellationToken)
    {
        var owner = UserId.Of(ownerId);

        return await db.SignalReportRules
            .Where(rule => rule.SignalId == signalId)
            .Where(rule => rule.OwnerId == owner)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid signalId, Guid accountId, string reportCode, CancellationToken cancellationToken) =>
        db.SignalReportRules
            .Where(rule => rule.SignalId == signalId)
            .Where(rule => rule.AccountId == accountId)
            .AnyAsync(rule => rule.ReportCode == reportCode, cancellationToken);

    public void Add(SignalReportRule rule) => db.SignalReportRules.Add(rule);

    public void Remove(SignalReportRule rule) => db.SignalReportRules.Remove(rule);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
