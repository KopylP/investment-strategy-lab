namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record DividendIncome(Guid TransactionId, DateOnly ReceivedOn, Guid InstrumentId, decimal Amount, decimal AmountPln, decimal FxRate, decimal GrossPln = 0m,
    decimal WithheldPln = 0m, string? Currency = null, bool WithholdingKnown = false, Guid AccountId = default);
