using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record RealizationDto(
    Guid SaleTransactionId,
    DateOnly SoldOn,
    Guid InstrumentId,
    decimal Quantity,
    decimal ProceedsInTaxCurrency,
    decimal CostInTaxCurrency,
    decimal GainInTaxCurrency,
    decimal FxRate,
    DateOnly? FxRateDate,
    IReadOnlyList<LotMatchDto> Lots,
    IReadOnlyList<GainAttributionDto> Attributions,
    decimal UnmatchedQuantity,
    decimal? BrokerGainInTaxCurrency,
    Guid AccountId)
{
    public static RealizationDto From(Realization realization) => new(
        SaleTransactionId: realization.SaleTransactionId,
        SoldOn: realization.SoldOn,
        InstrumentId: realization.InstrumentId,
        Quantity: realization.Quantity,
        ProceedsInTaxCurrency: realization.ProceedsInTaxCurrency,
        CostInTaxCurrency: realization.CostInTaxCurrency,
        GainInTaxCurrency: realization.GainInTaxCurrency,
        FxRate: realization.FxRate,
        FxRateDate: realization.FxRateDate,
        Lots: [.. realization.Lots.Select(LotMatchDto.From)],
        Attributions: [.. realization.Attributions.Select(GainAttributionDto.From)],
        UnmatchedQuantity: realization.UnmatchedQuantity,
        BrokerGainInTaxCurrency: realization.BrokerGainInTaxCurrency,
        AccountId: realization.AccountId);
}
