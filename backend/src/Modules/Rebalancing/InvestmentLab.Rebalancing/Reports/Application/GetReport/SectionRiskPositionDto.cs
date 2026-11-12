using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record SectionRiskPositionDto(
    Guid InstrumentId,
    string Status,
    decimal Quantity,
    decimal AverageCost,
    decimal CurrentPrice,
    decimal Value,
    decimal PnlPct,
    decimal DrawdownFromHigh,
    decimal? ExitPrice,
    string ExitRule,
    decimal? ExitDistance,
    IReadOnlyList<string> Alerts,
    bool Sell)
{
    public static SectionRiskPositionDto From(SectionRiskPosition sectionRiskPosition) => new(
        InstrumentId: sectionRiskPosition.InstrumentId,
        Status: sectionRiskPosition.Status,
        Quantity: sectionRiskPosition.Quantity,
        AverageCost: sectionRiskPosition.AverageCost,
        CurrentPrice: sectionRiskPosition.CurrentPrice,
        Value: sectionRiskPosition.Value,
        PnlPct: sectionRiskPosition.PnlPct,
        DrawdownFromHigh: sectionRiskPosition.DrawdownFromHigh,
        ExitPrice: sectionRiskPosition.ExitPrice,
        ExitRule: sectionRiskPosition.ExitRule,
        ExitDistance: sectionRiskPosition.ExitDistance,
        Alerts: sectionRiskPosition.Alerts,
        Sell: sectionRiskPosition.Sell);
}
