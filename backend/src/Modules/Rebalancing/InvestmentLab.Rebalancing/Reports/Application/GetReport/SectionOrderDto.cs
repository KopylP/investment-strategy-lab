using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record SectionOrderDto(
    string Kind,
    Guid InstrumentId,
    decimal Quantity,
    decimal Price,
    decimal Amount,
    string Reason)
{
    public static SectionOrderDto From(SectionOrder sectionOrder) => new(
        Kind: sectionOrder.Kind,
        InstrumentId: sectionOrder.InstrumentId,
        Quantity: sectionOrder.Quantity,
        Price: sectionOrder.Price,
        Amount: sectionOrder.Amount,
        Reason: sectionOrder.Reason);
}
