using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record LotMatchDto(
    Guid BuyTransactionId,
    DateOnly AcquiredOn,
    decimal Quantity,
    decimal CostInTaxCurrency,
    bool FromBroker,
    decimal Gross,
    decimal FxRate)
{
    public static LotMatchDto From(LotMatch lotMatch) => new(
        BuyTransactionId: lotMatch.BuyTransactionId,
        AcquiredOn: lotMatch.AcquiredOn,
        Quantity: lotMatch.Quantity,
        CostInTaxCurrency: lotMatch.CostInTaxCurrency,
        FromBroker: lotMatch.FromBroker,
        Gross: lotMatch.Gross,
        FxRate: lotMatch.FxRate);
}
