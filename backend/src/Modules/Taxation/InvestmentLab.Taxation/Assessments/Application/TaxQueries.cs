using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application;

public sealed record TaxYearListItemDto(int Year, string PolicyCode, DateTimeOffset ComputedAt, TaxYearSummary Summary);

/// <param name="IsTaxed">False for IKE / IKZE: its gains stay out of PIT-38.</param>
public sealed record TaxAccountDto(Guid Id, string Name, string Broker, string Currency, string PolicyCode, bool IsTaxed);

/// <summary>The taxpayer's years (newest first) and which of their accounts make up PIT-38.</summary>
public sealed record TaxOverviewDto(IReadOnlyList<TaxYearListItemDto> Years, IReadOnlyList<TaxAccountDto> Accounts);

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
    IReadOnlyList<TaxAccountDto> Accounts,
    IReadOnlyList<string> Warnings);

/// <summary>How the year's tax grows (the dashboard).</summary>
/// <param name="TaxDue">So far: every taxed account netted, as one PIT-38.</param>
/// <param name="Points">The year after each day with a sale or a dividend, oldest first.</param>
/// <param name="AlertShare">The person's warning threshold: tax as a share (0..1) of all accounts' value; null — off.</param>
/// <param name="Incomplete">What the figures miss (NBP rates, purchases of sold units): the amount may be too low.</param>
public sealed record TaxProgressDto(
    int Year, decimal TaxDue, decimal Gain, decimal Dividends, int TaxedAccounts, IReadOnlyList<TaxTimelinePoint> Points, decimal? AlertShare,
    IReadOnlyList<string> Incomplete);

/// <param name="AlertShare">Warn when this year's tax exceeds this share (0..1) of all accounts' value; null — off.</param>
public sealed record TaxSettingsDto(decimal? AlertShare);

public sealed record LossCarryforwardDto(Guid Id, int Year, decimal Amount, decimal AlreadyUsed);

public sealed record RecordLossCommand(int Year, decimal Amount, decimal AlreadyUsed = 0m);

/// <summary>
/// Tax years of the signed-in taxpayer over all their taxed accounts; each read recomputes first, so what the user sees
/// is never stale.
/// </summary>
public sealed class TaxQueries(TaxRecalculator recalculator, IAssessmentRepository assessments, IInstrumentCatalog instruments, ICurrentUser currentUser)
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
        var settings = await assessments.SettingsAsync(owner, cancellationToken);
        return new TaxProgressDto(year, summary?.TaxDue ?? 0m, summary?.Gain ?? 0m, summary?.DividendIncome ?? 0m, outcome.Taxpayer.Taxed.Count,
            TaxCalculator.Timeline(outcome.Events, outcome.Taxpayer.Policy, outcome.Losses, year), settings?.AlertShare, outcome.Incomplete);
    }

    public async Task<TaxSettingsDto> Settings(CancellationToken cancellationToken) =>
        new((await assessments.SettingsAsync(currentUser.RequiredId, cancellationToken))?.AlertShare);

    public async Task<Result<TaxSettingsDto>> ChangeSettings(TaxSettingsDto command, CancellationToken cancellationToken)
    {
        var owner = currentUser.RequiredId;
        var settings = await assessments.SettingsAsync(owner, cancellationToken);
        if (settings is null)
        {
            settings = TaxSettings.For(owner);
            assessments.AddSettings(settings);
        }
        var changed = settings.Change(command.AlertShare);
        if (changed.IsFailure)
            return changed.Error;
        await assessments.SaveChangesAsync(cancellationToken);
        return new TaxSettingsDto(settings.AlertShare);
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
