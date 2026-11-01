namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record DividendIncome(
    Guid TransactionId,
    DateOnly ReceivedOn,
    Guid InstrumentId,
    decimal Amount,
    decimal AmountInTaxCurrency,
    decimal FxRate,
    decimal GrossInTaxCurrency = 0m,

    decimal WithheldInTaxCurrency = 0m, string? Currency = null, bool WithholdingKnown = false, Guid AccountId = default);
