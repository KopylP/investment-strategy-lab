using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application;

public sealed record TaxYearListItemDto(int Year, string PolicyCode, DateTimeOffset ComputedAt, TaxYearSummary Summary);

public sealed record TaxYearDetailsDto(
    int Year,
    string PolicyCode,
    string FxRule,
    DateTimeOffset ComputedAt,
    TaxYearSummary Summary,
    IReadOnlyList<Realization> Realizations,
    IReadOnlyList<DividendIncome> Dividends,
    IReadOnlyList<CarryforwardUse> CarryforwardUses,
    IReadOnlyList<OpenLotSnapshot> OpenLots,
    IReadOnlyDictionary<Guid, string> Instruments,
    IReadOnlyList<string> Warnings);

public sealed record LossCarryforwardDto(Guid Id, int Year, decimal Amount, decimal AlreadyUsed);

public sealed record RecordLossCommand(int Year, decimal Amount, decimal AlreadyUsed = 0m);

/// <summary>Tax years of an account; each read recomputes first, so what the user sees is never stale.</summary>
public sealed class TaxQueries(TaxRecalculator recalculator, TaxInputs inputs, IAssessmentRepository assessments, IInstrumentCatalog instruments)
{
    private const string YahooPrefix = "yfinance:";

    public async Task<Result<IReadOnlyList<TaxYearListItemDto>>> Years(Guid accountId, CancellationToken cancellationToken)
    {
        var outcome = await recalculator.RecomputeAsync(accountId, cancellationToken);
        if (outcome.IsFailure)
            return outcome.Error;
        return Result<IReadOnlyList<TaxYearListItemDto>>.Success([.. (await assessments.ForAccountAsync(accountId, cancellationToken))
            .OrderByDescending(assessment => assessment.Year)
            .Select(assessment => new TaxYearListItemDto(assessment.Year, assessment.PolicyCode, assessment.ComputedAt, assessment.Summary))]);
    }

    public async Task<Result<TaxYearDetailsDto>> Year(Guid accountId, int year, CancellationToken cancellationToken)
    {
        var outcome = await recalculator.RecomputeAsync(accountId, cancellationToken);
        if (outcome.IsFailure)
            return outcome.Error;
        if ((await assessments.ForAccountAsync(accountId, cancellationToken)).FirstOrDefault(assessment => assessment.Year == year) is not { } assessment)
            return Error.NotFound("tax.year_not_found", $"No taxable events in {year}.");

        var account = (await inputs.EnsureAccountAsync(accountId, cancellationToken))!;
        var policy = TaxPolicies.For(account.PolicyCode);
        var openLots = year == DateTime.UtcNow.Year
            ? TaxCalculator.Compute((await inputs.EventsAsync(account, cancellationToken)).Events, policy, await inputs.LossesAsync(accountId, cancellationToken)).OpenLots
            : [];

        var catalog = (await instruments.GetInstruments(cancellationToken)).ToDictionary(instrument => instrument.Id);
        var ids = assessment.Realizations.Select(r => r.InstrumentId).Concat(assessment.Dividends.Select(d => d.InstrumentId)).Concat(openLots.Select(lot => lot.InstrumentId)).Distinct();
        var labels = ids.ToDictionary(id => id, id => catalog.TryGetValue(id, out var instrument)
            ? instrument.ProviderSymbols.FirstOrDefault(symbol => symbol.StartsWith(YahooPrefix, StringComparison.Ordinal))?[YahooPrefix.Length..] ?? instrument.Name
            : id.ToString()[..8]);

        return new TaxYearDetailsDto(year, assessment.PolicyCode, policy.FxRule, assessment.ComputedAt, assessment.Summary, assessment.Realizations,
            assessment.Dividends, assessment.CarryforwardUses, openLots, labels, outcome.Value.Warnings);
    }

    public async Task<IReadOnlyList<LossCarryforwardDto>> Losses(Guid accountId, CancellationToken cancellationToken) =>
        [.. (await assessments.LossesAsync(accountId, cancellationToken)).OrderBy(loss => loss.Year).Select(loss => new LossCarryforwardDto(loss.Id, loss.Year, loss.Amount, loss.AlreadyUsed))];

    public async Task<Result<LossCarryforwardDto>> RecordLoss(Guid accountId, RecordLossCommand command, CancellationToken cancellationToken)
    {
        if (await inputs.EnsureAccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");
        var loss = LossCarryforward.Record(accountId, account.OwnerId, command.Year, command.Amount, command.AlreadyUsed);
        if (loss.IsFailure)
            return loss.Error;
        assessments.AddLoss(loss.Value);
        await assessments.SaveChangesAsync(cancellationToken);
        await recalculator.RecomputeAsync(accountId, cancellationToken);
        return new LossCarryforwardDto(loss.Value.Id, loss.Value.Year, loss.Value.Amount, loss.Value.AlreadyUsed);
    }

    public async Task<Result> RemoveLoss(Guid accountId, Guid lossId, CancellationToken cancellationToken)
    {
        if ((await assessments.LossesAsync(accountId, cancellationToken)).FirstOrDefault(loss => loss.Id == lossId) is not { } loss)
            return Error.NotFound("tax.loss_not_found", $"Loss {lossId} does not exist.");
        assessments.RemoveLoss(loss);
        await assessments.SaveChangesAsync(cancellationToken);
        await recalculator.RecomputeAsync(accountId, cancellationToken);
        return Result.Success();
    }
}
