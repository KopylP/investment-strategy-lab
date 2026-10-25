using InvestmentLab.Taxation.Assessments.Application;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Taxation.Assessments.Infrastructure;

internal sealed class AssessmentRepository(TaxationDbContext db) : IAssessmentRepository
{
    public async Task<IReadOnlyList<TaxAssessment>> ForAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        await db.Assessments.Where(assessment => assessment.AccountId == accountId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LossCarryforward>> LossesAsync(Guid accountId, CancellationToken cancellationToken) =>
        await db.Losses.Where(loss => loss.AccountId == accountId).ToListAsync(cancellationToken);

    public void Add(TaxAssessment assessment) => db.Assessments.Add(assessment);

    public void AddLoss(LossCarryforward loss) => db.Losses.Add(loss);

    public void RemoveLoss(LossCarryforward loss) => db.Losses.Remove(loss);

    public void Publish<TEvent>(TEvent integrationEvent, DateTimeOffset occurredAt) where TEvent : class => db.Publish(integrationEvent, occurredAt);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
