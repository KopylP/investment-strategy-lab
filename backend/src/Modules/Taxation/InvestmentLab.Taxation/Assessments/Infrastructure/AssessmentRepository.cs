using InvestmentLab.Taxation.Assessments.Application;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Taxation.Assessments.Infrastructure;

internal sealed class AssessmentRepository(TaxationDbContext db) : IAssessmentRepository
{
    public async Task<IReadOnlyList<TaxAssessment>> ForOwnerAsync(UserId ownerId, CancellationToken cancellationToken) =>
        await db.Assessments.Where(assessment => assessment.OwnerId == ownerId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LossCarryforward>> LossesAsync(UserId ownerId, CancellationToken cancellationToken) =>
        await db.Losses.Where(loss => loss.OwnerId == ownerId).ToListAsync(cancellationToken);

    public void Add(TaxAssessment assessment) => db.Assessments.Add(assessment);

    public void AddLoss(LossCarryforward loss) => db.Losses.Add(loss);

    public void RemoveLoss(LossCarryforward loss) => db.Losses.Remove(loss);

    public void Publish<TEvent>(TEvent integrationEvent, DateTimeOffset occurredAt) where TEvent : class => db.Publish(integrationEvent, occurredAt);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    // Retries (Aspire's Npgsql execution strategy) repeat the whole unit, lock included.
    public Task<T> SerializedAsync<T>(UserId ownerId, Func<Task<T>> work, CancellationToken cancellationToken) =>
        db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var owner = BitConverter.ToInt32(ownerId.Value.ToByteArray(), 0);
            await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock({TaxpayerLockClass}, {owner})", cancellationToken);
            var result = await work();
            await transaction.CommitAsync(cancellationToken);
            return result;
        });

    /// <summary>First key of the two-key advisory lock, so taxpayer locks never meet other modules' locks.</summary>
    private const int TaxpayerLockClass = 0x7A78;
}
