namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record Realization(
    Guid SaleTransactionId,
    DateOnly SoldOn,
    Guid InstrumentId,
    decimal Quantity,
    decimal ProceedsInTaxCurrency,
    decimal CostInTaxCurrency,
    decimal GainInTaxCurrency,
    decimal FxRate,
    DateOnly? FxRateDate,
    IReadOnlyList<LotMatch> Lots,
    IReadOnlyList<GainAttribution> Attributions,
    decimal UnmatchedQuantity,
    decimal? BrokerGainInTaxCurrency = null,
    Guid AccountId = default);
