using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Taxation.Assessments.Application.Shared;
using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.GetTaxYear;

public sealed class GetTaxYearHandler(IAssessmentRepository assessments, IInstrumentCatalog instruments, TimeProvider clock)
{
    private const string YahooPrefix = "yfinance:";
    private const int ShortIdLength = 8;

    public async Task<Result<TaxYearDetailsDto>> Handle(int year, RecomputeOutcome refreshed, CancellationToken cancellationToken)
    {
        var assessed = await assessments.ForOwnerAsync(refreshed.Taxpayer.OwnerId, cancellationToken);
        if (assessed.FirstOrDefault(assessment => assessment.Year == year) is not { } assessment)
            return Error.NotFound("tax.year_not_found", $"No taxable events in {year}.");

        var openLots = year == clock.GetUtcNow().Year ? refreshed.Computation.OpenLots : [];
        var labels = await LabelsAsync(assessment, openLots, cancellationToken);

        return new TaxYearDetailsDto(
            year,
            assessment.PolicyCode,
            refreshed.Taxpayer.Policy.FxRule,
            assessment.ComputedAt,
            assessment.Summary,
            assessment.Realizations,
            assessment.Dividends,
            assessment.CarryforwardUses,
            openLots,
            labels,
            TaxAccountMapping.AccountsOf(refreshed.Taxpayer),
            refreshed.Warnings);
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

        var yahooSymbol = instrument.ProviderSymbols.FirstOrDefault(symbol => symbol.StartsWith(YahooPrefix, StringComparison.Ordinal));

        return yahooSymbol?[YahooPrefix.Length..] ?? instrument.Name;
    }
}
