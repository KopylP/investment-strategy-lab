namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record OpenLotSnapshot(
    Guid BuyTransactionId,
    DateOnly AcquiredOn,
    Guid InstrumentId,
    decimal Quantity,
    decimal CostInTaxCurrency,
    decimal FxRate,
    Guid AccountId = default);
