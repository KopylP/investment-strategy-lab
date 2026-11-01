using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Rebalancing.Reports.Application.Shared;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed class GetReportHandler(IReportRepository reports, IInstrumentCatalog instruments)
{

    public async Task<Result<ReportDetailsDto>> Handle(Guid reportId, CancellationToken cancellationToken)
    {
        if (reportId == Guid.Empty || await reports.FindAsync(ReportId.Of(reportId), cancellationToken) is not { } report)
            return ReportErrors.NotFound(reportId);

        var labels = await LabelsOfInstrumentsUsedAsync(report, cancellationToken);

        return new ReportDetailsDto(
            Summary: report.ToSummaryDto(),
            ContributionPlan: report.Contribution,
            Actions: [.. report.Actions.Select(ToDto)],
            Sections: report.Sections,
            Warnings: report.Warnings,
            Review: report.Review,
            TaxEstimate: report.TaxEstimate,
            Instruments: labels,
            SubPortfolios: report.Sections.ToDictionary(section => section.SubPortfolioId, section => section.Name));
    }

    private async Task<Dictionary<Guid, InstrumentLabelDto>> LabelsOfInstrumentsUsedAsync(
        RebalancingReport report,
        CancellationToken cancellationToken)
    {
        var known = await instruments.GetInstruments(cancellationToken);
        var catalog = known.ToDictionary(instrument => instrument.Id);

        return InstrumentsUsedBy(report)
            .Where(catalog.ContainsKey)
            .ToDictionary(id => id, id => LabelOf(catalog[id]));
    }

    private static IEnumerable<Guid> InstrumentsUsedBy(RebalancingReport report)
    {
        var inActions = report.Actions.Select(action => action.InstrumentId);
        var inSections = report.Sections.SelectMany(InstrumentsUsedBy);

        return inActions.Concat(inSections).OfType<Guid>().Distinct();
    }

    private static IEnumerable<Guid?> InstrumentsUsedBy(ReportSection section)
    {
        var orders = section.Orders.Select(order => (Guid?)order.InstrumentId);
        var allocation = section.Allocation.Select(row => row.InstrumentId);
        var scores = section.Decision?.Scores.Select(score => (Guid?)score.InstrumentId) ?? [];
        var positions = section.Risk?.Positions.Select(position => (Guid?)position.InstrumentId) ?? [];

        return orders.Concat(allocation).Concat(scores).Concat(positions);
    }

    private static InstrumentLabelDto LabelOf(InstrumentDto instrument) =>
        new(SymbolOf(instrument), instrument.Name, instrument.Isin, instrument.TradingCurrency);

    private static string SymbolOf(InstrumentDto instrument)
    {
        return instrument.Label;
    }

    private static ReportActionDto ToDto(ReportAction action) => new(
        Id: action.Id.Value,
        Sequence: action.Sequence,
        Kind: action.Kind.ToString(),
        InstrumentId: action.InstrumentId,
        PlannedQuantity: action.PlannedQuantity,
        PlannedPrice: action.PlannedPrice,
        PlannedAmount: action.PlannedAmount,
        Allocations: action.Allocations,
        FromSubPortfolioId: action.FromSubPortfolioId,
        ToSubPortfolioId: action.ToSubPortfolioId,
        Reason: action.Reason,
        Status: action.Status.ToString(),
        FilledQuantity: action.FilledQuantity,
        RemainingQuantity: action.RemainingQuantity,
        Fills: action.Fills,
        SkipReason: action.SkipReason,
        Rejection: action.Rejection,
        EstimatedGain: action.EstimatedGain,
        EstimatedTaxChange: action.EstimatedTaxChange,
        EstimatedCarriedLoss: action.EstimatedCarriedLoss);
}
