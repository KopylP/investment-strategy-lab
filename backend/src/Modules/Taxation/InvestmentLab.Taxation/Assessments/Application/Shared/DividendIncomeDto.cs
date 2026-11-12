using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record DividendIncomeDto(
    Guid TransactionId,
    DateOnly ReceivedOn,
    Guid InstrumentId,
    decimal Amount,
    decimal AmountInTaxCurrency,
    decimal FxRate,
    decimal GrossInTaxCurrency,
    decimal WithheldInTaxCurrency,
    string? Currency,
    bool WithholdingKnown,
    Guid AccountId)
{
    public static DividendIncomeDto From(DividendIncome dividendIncome) => new(
        TransactionId: dividendIncome.TransactionId,
        ReceivedOn: dividendIncome.ReceivedOn,
        InstrumentId: dividendIncome.InstrumentId,
        Amount: dividendIncome.Amount,
        AmountInTaxCurrency: dividendIncome.AmountInTaxCurrency,
        FxRate: dividendIncome.FxRate,
        GrossInTaxCurrency: dividendIncome.GrossInTaxCurrency,
        WithheldInTaxCurrency: dividendIncome.WithheldInTaxCurrency,
        Currency: dividendIncome.Currency,
        WithholdingKnown: dividendIncome.WithholdingKnown,
        AccountId: dividendIncome.AccountId);
}
