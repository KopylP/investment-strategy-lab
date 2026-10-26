using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Contracts;

namespace InvestmentLab.Taxation.Assessments.Application;

/// <param name="Computation">The whole computation (open lots included), so readers need not repeat it.</param>
public sealed record RecomputeOutcome(IReadOnlyList<int> ChangedYears, IReadOnlyList<string> Warnings, Taxpayer Taxpayer, TaxComputation Computation);

/// <summary>
/// Rebuilds every tax year of a taxpayer from the ledgers of all their taxed accounts (§6.7 RecomputeAssessment): one
/// PIT-38 nets gains and losses of every broker. Unchanged years stay as they are.
/// </summary>
public sealed class TaxRecalculator(TaxInputs inputs, IAssessmentRepository assessments, TimeProvider clock)
{
    /// <summary>A change in one account: its owner's years are rebuilt.</summary>
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
        // A year whose last taxable entry was reversed (or whose account stopped being taxed) is recomputed to zero rather than left stale.
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
                assessments.Publish(new TaxAssessmentUpdatedV2(ownerId.Value, number, year.Summary.TaxDue, now), now);
            }
        }

        await assessments.SaveChangesAsync(cancellationToken);

        List<string> warnings = [.. computation.Warnings];
        if (missing.Count > 0)
            warnings.Insert(0, $"Немає курсу NBP для {missing.Count} операцій ({string.Join(", ", missing.Take(5))}…): вони не враховані. Оновіть курси NBP.");
        return new RecomputeOutcome(changed, warnings, taxpayer, computation);
    }
}
