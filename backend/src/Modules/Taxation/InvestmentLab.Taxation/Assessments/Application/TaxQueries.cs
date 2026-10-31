using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application;

public sealed class TaxQueries(TaxRecalculator recalculator, IAssessmentRepository assessments, IInstrumentCatalog instruments, ICurrentUser currentUser, TaxAlertOptions alert)
{
    private const string YahooPrefix = "yfinance:";

    public async Task<TaxOverviewDto> Overview(CancellationToken cancellationToken)
    {
        var owner = currentUser.RequiredId;
        var outcome = await recalculator.RecomputeOwnerAsync(owner, cancellationToken);

        return new TaxOverviewDto(
            [.. (await assessments.ForOwnerAsync(owner, cancellationToken))
                .OrderByDescending(assessment => assessment.Year)
                .Select(assessment => new TaxYearListItemDto(assessment.Year, assessment.PolicyCode, assessment.ComputedAt, assessment.Summary))],
            AccountsOf(outcome.Taxpayer));
    }

    public async Task<Result<TaxYearDetailsDto>> Year(int year, CancellationToken cancellationToken)
    {
        var owner = currentUser.RequiredId;
        var outcome = await recalculator.RecomputeOwnerAsync(owner, cancellationToken);

        if ((await assessments.ForOwnerAsync(owner, cancellationToken)).FirstOrDefault(assessment => assessment.Year == year) is not { } assessment)
            return Error.NotFound("tax.year_not_found", $"No taxable events in {year}.");

        var openLots = year == DateTime.UtcNow.Year ? outcome.Computation.OpenLots : [];

        var catalog = (await instruments.GetInstruments(cancellationToken)).ToDictionary(instrument => instrument.Id);
        var ids = assessment.Realizations.Select(r => r.InstrumentId).Concat(assessment.Dividends.Select(d => d.InstrumentId)).Concat(openLots.Select(lot => lot.InstrumentId)).Distinct();
        var labels = ids.ToDictionary(id => id, id => catalog.TryGetValue(id, out var instrument)
            ? instrument.ProviderSymbols.FirstOrDefault(symbol => symbol.StartsWith(YahooPrefix, StringComparison.Ordinal))?[YahooPrefix.Length..] ?? instrument.Name
            : id.ToString()[..8]);

        return new TaxYearDetailsDto(year, assessment.PolicyCode, outcome.Taxpayer.Policy.FxRule, assessment.ComputedAt, assessment.Summary, assessment.Realizations,
            assessment.Dividends, assessment.CarryforwardUses, openLots, labels, AccountsOf(outcome.Taxpayer), outcome.Warnings);
    }

    public async Task<TaxProgressDto> Progress(int year, CancellationToken cancellationToken)
    {
        var owner = currentUser.RequiredId;
        var outcome = await recalculator.RecomputeOwnerAsync(owner, cancellationToken);
        var summary = outcome.Computation.Year(year)?.Summary;

        return new TaxProgressDto(year, summary?.TaxDue ?? 0m, summary?.Gain ?? 0m, summary?.DividendIncome ?? 0m, outcome.Taxpayer.Taxed.Count,
            TaxCalculator.Timeline(outcome.Events, outcome.Taxpayer.Policy, outcome.Losses, year), alert.Share, outcome.Incomplete);
    }

    public async Task<IReadOnlyList<LossCarryforwardDto>> Losses(CancellationToken cancellationToken) =>
        [.. (await assessments.LossesAsync(currentUser.RequiredId, cancellationToken)).OrderBy(loss => loss.Year)
            .Select(loss => new LossCarryforwardDto(loss.Id, loss.Year, loss.Amount, loss.AlreadyUsed))];

    public async Task<Result<LossCarryforwardDto>> RecordLoss(RecordLossCommand command, CancellationToken cancellationToken)
    {
        var owner = currentUser.RequiredId;
        var loss = LossCarryforward.Record(owner, command.Year, command.Amount, command.AlreadyUsed);
        if (loss.IsFailure)
            return loss.Error;

        assessments.AddLoss(loss.Value);
        await assessments.SaveChangesAsync(cancellationToken);
        await recalculator.RecomputeOwnerAsync(owner, cancellationToken);

        return new LossCarryforwardDto(loss.Value.Id, loss.Value.Year, loss.Value.Amount, loss.Value.AlreadyUsed);
    }

    public async Task<Result> RemoveLoss(Guid lossId, CancellationToken cancellationToken)
    {
        var owner = currentUser.RequiredId;
        if ((await assessments.LossesAsync(owner, cancellationToken)).FirstOrDefault(loss => loss.Id == lossId) is not { } loss)
            return Error.NotFound("tax.loss_not_found", $"Loss {lossId} does not exist.");

        assessments.RemoveLoss(loss);
        await assessments.SaveChangesAsync(cancellationToken);
        await recalculator.RecomputeOwnerAsync(owner, cancellationToken);

        return Result.Success();
    }

    private static IReadOnlyList<TaxAccountDto> AccountsOf(Taxpayer taxpayer) =>
        [.. taxpayer.Accounts.Select(account =>
        {
            var setup = taxpayer.Setups.GetValueOrDefault(account.AccountId);

            return new TaxAccountDto(account.AccountId, setup?.Name ?? "", setup?.Broker ?? "", account.Currency, account.PolicyCode, TaxPolicies.For(account.PolicyCode).IsTaxed);
        })];
}
