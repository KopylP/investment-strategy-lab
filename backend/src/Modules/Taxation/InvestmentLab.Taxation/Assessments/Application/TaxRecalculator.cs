using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Contracts;

namespace InvestmentLab.Taxation.Assessments.Application;

/// <param name="Incomplete">What the figures are missing (NBP rates, purchases of sold units): a return is not filled from them.</param>
public sealed record RecomputeOutcome(IReadOnlyList<int> ChangedYears, IReadOnlyList<string> Warnings, IReadOnlyList<string> Incomplete);

/// <summary>Rebuilds every tax year of an account from the ledger (§6.7 RecomputeAssessment); unchanged years stay as they are.</summary>
public sealed class TaxRecalculator(TaxInputs inputs, IAssessmentRepository assessments, TimeProvider clock)
{
    public async Task<Result<RecomputeOutcome>> RecomputeAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (await inputs.EnsureAccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var policy = TaxPolicies.For(account.PolicyCode);
        var (events, missing) = await inputs.EventsAsync(account, cancellationToken);
        var losses = await inputs.LossesAsync(accountId, cancellationToken);
        var computation = TaxCalculator.Compute(events, policy, losses);

        var now = clock.GetUtcNow();
        var existing = (await assessments.ForAccountAsync(accountId, cancellationToken)).ToDictionary(assessment => assessment.Year);
        var changed = new List<int>();
        // A year whose last taxable entry was reversed is recomputed to zero rather than left stale.
        var emptied = existing.Keys.Where(year => computation.Year(year) is null)
            .Select(year => new TaxYearResult(new TaxYearSummary(year, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m), [], [], []));
        foreach (var year in computation.Years.Concat(emptied))
        {
            var number = year.Summary.Year;
            if (!existing.TryGetValue(number, out var assessment))
            {
                assessment = TaxAssessment.Start(accountId, account.OwnerId, number);
                assessments.Add(assessment);
            }
            if (assessment.Recompute(year, policy, TaxInputs.HashFor(number, events, losses, policy), now))
            {
                changed.Add(number);
                assessments.Publish(new TaxAssessmentUpdatedV1(accountId, account.OwnerId.Value, number, year.Summary.TaxDue, now), now);
            }
        }

        await assessments.SaveChangesAsync(cancellationToken);

        List<string> warnings = [.. computation.Warnings];
        if (missing.Count > 0)
            warnings.Insert(0, $"Немає курсу NBP для {missing.Count} операцій ({string.Join(", ", missing.Take(5))}…): вони не враховані. Оновіть курси NBP.");
        return new RecomputeOutcome(changed, warnings, warnings);
    }
}
