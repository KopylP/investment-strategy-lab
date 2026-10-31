using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record TaxEvent(
    Guid TransactionId,
    DateOnly OccurredOn,
    int Sequence,
    TaxableKind Kind,
    Guid InstrumentId,
    decimal Quantity,
    decimal Price,
    decimal Fee,
    decimal Amount,
    IReadOnlyList<EntryAllocation> Allocations,
    decimal FxRate,
    DateOnly? FxRateDate,
    SaleFacts? Broker = null,
    WithholdingFacts? Withholding = null,
    string? Currency = null,
    Guid AccountId = default);
