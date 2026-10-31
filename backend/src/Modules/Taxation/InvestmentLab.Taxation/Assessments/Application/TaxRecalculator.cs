using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application;

public sealed class TaxRecalculator(TaxInputs inputs, IAssessmentRepository assessments, TimeProvider clock)
{
    public async Task<Result<RecomputeOutcome>> RecomputeAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (await inputs.OwnerOfAsync(accountId, cancellationToken) is not { } ownerId)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        return await RecomputeOwnerAsync(ownerId, cancellationToken);
    }

    public Task<RecomputeOutcome> RecomputeOwnerAsync(UserId ownerId, CancellationToken cancellationToken) =>
        assessments.SerializedAsync(ownerId, () => RecomputeLockedAsync(ownerId, cancellationToken), cancellationToken);

    private async Task<RecomputeOutcome> RecomputeLockedAsync(UserId ownerId, CancellationToken cancellationToken)
    {
        var taxpayer = await inputs.TaxpayerAsync(ownerId, cancellationToken);
        var policy = taxpayer.Policy;
        var (events, missing) = await inputs.EventsAsync(taxpayer.Taxed, cancellationToken);
        var losses = await inputs.LossesAsync(ownerId, cancellationToken);
        var computation = TaxCalculator.Compute(events, policy, losses);

        var now = clock.GetUtcNow();
        var existing = (await assessments.ForOwnerAsync(ownerId, cancellationToken)).ToDictionary(assessment => assessment.Year);
        var changed = new List<int>();

        var emptied = existing.Keys.Where(year => computation.Year(year) is null)
            .Select(year => new TaxYearResult(new TaxYearSummary(year, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m), [], [], []));
        foreach (var year in computation.Years.Concat(emptied))
        {
            var number = year.Summary.Year;
            if (!existing.TryGetValue(number, out var assessment))
            {
                assessment = TaxAssessment.Start(ownerId, number);
                assessments.Add(assessment);
            }

            if (assessment.Recompute(year, policy, TaxInputs.HashFor(number, events, losses, policy), now))
            {
                changed.Add(number);
            }
        }

        await assessments.SaveChangesAsync(cancellationToken);

        List<string> warnings = [.. computation.Warnings];

        if (missing.Count > 0)
            warnings.Insert(0, $"Немає курсу NBP для {missing.Count} операцій ({string.Join(", ", missing.Take(5))}…): вони не враховані. Оновіть курси NBP.");

        return new RecomputeOutcome(changed, warnings, warnings, taxpayer, computation, events, losses);
    }
}
