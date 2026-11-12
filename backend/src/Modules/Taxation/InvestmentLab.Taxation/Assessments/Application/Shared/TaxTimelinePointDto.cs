using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record TaxTimelinePointDto(
    DateOnly Date,
    decimal Gain,
    decimal DividendIncome,
    decimal TaxDue)
{
    public static TaxTimelinePointDto From(TaxTimelinePoint taxTimelinePoint) => new(
        Date: taxTimelinePoint.Date,
        Gain: taxTimelinePoint.Gain,
        DividendIncome: taxTimelinePoint.DividendIncome,
        TaxDue: taxTimelinePoint.TaxDue);
}
