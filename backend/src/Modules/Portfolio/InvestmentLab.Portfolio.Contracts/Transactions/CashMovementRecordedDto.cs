namespace InvestmentLab.Portfolio.Contracts.Transactions;

public sealed record CashMovementRecordedDto(
    string Kind, decimal Amount, Guid? InstrumentId, IReadOnlyList<AmountAllocationDto> Allocations,
    decimal? SettledAmount = null, string? SettledCurrency = null);
