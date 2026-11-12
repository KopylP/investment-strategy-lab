using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record OpenLotSnapshotDto(
    Guid BuyTransactionId,
    DateOnly AcquiredOn,
    Guid InstrumentId,
    decimal Quantity,
    decimal CostInTaxCurrency,
    decimal FxRate,
    Guid AccountId)
{
    public static OpenLotSnapshotDto From(OpenLotSnapshot openLotSnapshot) => new(
        BuyTransactionId: openLotSnapshot.BuyTransactionId,
        AcquiredOn: openLotSnapshot.AcquiredOn,
        InstrumentId: openLotSnapshot.InstrumentId,
        Quantity: openLotSnapshot.Quantity,
        CostInTaxCurrency: openLotSnapshot.CostInTaxCurrency,
        FxRate: openLotSnapshot.FxRate,
        AccountId: openLotSnapshot.AccountId);
}
