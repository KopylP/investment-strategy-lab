using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Domain;

public interface IAssessmentRepository
{
    Task<IReadOnlyList<TaxAssessment>> ForOwnerAsync(UserId ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LossCarryforward>> LossesAsync(UserId ownerId, CancellationToken cancellationToken);

    void Add(TaxAssessment assessment);

    void AddLoss(LossCarryforward loss);

    void RemoveLoss(LossCarryforward loss);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<T> SerializedAsync<T>(UserId ownerId, Func<Task<T>> work, CancellationToken cancellationToken);
}
