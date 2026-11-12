using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed class TaxRecalculator(
    IAccountSetupQueries accounts,
    TaxpayerReader taxpayers,
    TaxEventReader taxEvents,
    IAssessmentRepository assessments,
    TimeProvider clock)
{
    private const int ShownMissingRates = 5;

    private RecomputeOutcome? _refreshed;

    public async Task<Result<RecomputeOutcome>> RecomputeAsync(Guid accountId, CancellationToken cancellationToken)
    {
        if (await accounts.GetAccountSetup(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        return await RecomputeOwnerAsync(UserId.Of(account.OwnerId), cancellationToken);
    }

    public async Task<RecomputeOutcome> RecomputeOwnerAsync(UserId ownerId, CancellationToken cancellationToken)
    {
        _refreshed = await assessments.SerializedAsync(ownerId, () => RecomputeLockedAsync(ownerId, cancellationToken), cancellationToken);

        return _refreshed;
    }

    public async Task<RecomputeOutcome> RefreshedAsync(UserId ownerId, CancellationToken cancellationToken)
    {
        if (_refreshed is { } refreshed && refreshed.Taxpayer.OwnerId == ownerId)
            return refreshed;

        return await RecomputeOwnerAsync(ownerId, cancellationToken);
    }

    private async Task<RecomputeOutcome> RecomputeLockedAsync(UserId ownerId, CancellationToken cancellationToken)
    {
        var taxpayer = await taxpayers.ReadAsync(ownerId, cancellationToken);
        var policy = taxpayer.Policy;
        var (events, missing) = await taxEvents.EventsAsync(taxpayer.Taxed, cancellationToken);
        var losses = await taxpayers.LossesAsync(ownerId, cancellationToken);
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

            if (assessment.Recompute(year, policy, AssessmentHash.For(number, events, losses, policy), now))
            {
                changed.Add(number);
            }
        }

        await assessments.SaveChangesAsync(cancellationToken);

        List<string> warnings = [.. computation.Warnings];

        if (missing.Count > 0)
            warnings.Insert(0, MissingRatesWarning(taxpayer.Policy.FxRule.SourceName, missing));

        return new RecomputeOutcome(changed, warnings, warnings, taxpayer, computation, events, losses);
    }

    private static string MissingRatesWarning(string source, IReadOnlyList<string> missing)
    {
        var shown = string.Join(", ", missing.Take(ShownMissingRates));

        return Message.Of("tax.warning.rates_missing", new { source, count = missing.Count, shown });
    }
}
