using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Contracts;

namespace InvestmentLab.Taxation.Assessments.Application.Integration;

internal sealed class TaxAssessmentRecomputedMapper : IDomainEventMapper<TaxAssessmentRecomputed, TaxAssessmentUpdatedV2>
{
    public TaxAssessmentUpdatedV2 Map(TaxAssessmentRecomputed recomputed) =>
        new(recomputed.OwnerId.Value, recomputed.Year, recomputed.TaxDue, recomputed.OccurredAt);
}
