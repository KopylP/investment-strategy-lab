namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record Realization(
    Guid SaleTransactionId,
    DateOnly SoldOn,
    Guid InstrumentId,
    decimal Quantity,
    decimal ProceedsPln,
    decimal CostPln,
    decimal GainPln,
    decimal FxRate,
    DateOnly? FxRateDate,
    IReadOnlyList<LotMatch> Lots,
    IReadOnlyList<GainAttribution> Attributions,
    decimal UnmatchedQuantity,
    decimal? BrokerGainPln = null,
    Guid AccountId = default);
