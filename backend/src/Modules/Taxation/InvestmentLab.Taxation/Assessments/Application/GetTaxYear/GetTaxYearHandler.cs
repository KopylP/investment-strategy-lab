using InvestmentLab.SharedKernel;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.GetTaxYear;

public sealed class GetTaxYearHandler(
    IAssessmentRepository assessments,
    IInstrumentCatalog instruments,
    TaxRecalculator recalculator,
    ICurrentUser currentUser,
    TimeProvider clock) : IHandler
{
    private const int ShortIdLength = 8;

    public async Task<Result<TaxYearDetailsDto>> Handle(int year, CancellationToken cancellationToken)
    {
        var refreshed = await recalculator.RefreshedAsync(currentUser.RequiredId, cancellationToken);
        var assessed = await assessments.ForOwnerAsync(refreshed.Taxpayer.OwnerId, cancellationToken);
        if (assessed.FirstOrDefault(assessment => assessment.Year == year) is not { } assessment)
            return Error.NotFound("tax.year_not_found", $"No taxable events in {year}.");

        var openLots = year == clock.GetUtcNow().Year ? refreshed.Computation.OpenLots : [];
        var labels = await LabelsAsync(assessment, openLots, cancellationToken);
        var policy = TaxPolicies.For(assessment.PolicyCode);

        return new TaxYearDetailsDto(
            year,
            assessment.PolicyCode,
            policy.FxRule.Description,
            assessment.ComputedAt,
            TaxYearSummaryDto.From(assessment.Summary),
            [.. assessment.Realizations.Select(RealizationDto.From)],
            [.. assessment.Dividends.Select(DividendIncomeDto.From)],
            [.. assessment.CarryforwardUses.Select(CarryforwardUseDto.From)],
            [.. openLots.Select(OpenLotSnapshotDto.From)],
            labels,
            TaxAccountMapping.AccountsOf(refreshed.Taxpayer),
            refreshed.Warnings,
            policy.IsTaxed,
            policy.RateSchedule.Label);
    }

    private async Task<Dictionary<Guid, string>> LabelsAsync(
        TaxAssessment assessment,
        IReadOnlyList<OpenLotSnapshot> openLots,
        CancellationToken cancellationToken)
    {
        var known = await instruments.GetInstruments(cancellationToken);
        var catalog = known.ToDictionary(instrument => instrument.Id);

        var ids = assessment.Realizations.Select(realization => realization.InstrumentId)
            .Concat(assessment.Dividends.Select(dividend => dividend.InstrumentId))
            .Concat(openLots.Select(lot => lot.InstrumentId))
            .Distinct();

        return ids.ToDictionary(id => id, id => LabelOf(id, catalog));
    }

    private static string LabelOf(Guid id, Dictionary<Guid, InstrumentDto> catalog)
    {
        if (!catalog.TryGetValue(id, out var instrument))
            return id.ToString()[..ShortIdLength];

        return instrument.Label;
    }
}
