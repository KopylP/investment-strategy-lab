namespace InvestmentLab.Portfolio.Contracts.Transactions;

public sealed record TradeRecordedDto(
    Guid InstrumentId, string Side, decimal Quantity, decimal Price, decimal Fee, IReadOnlyList<QuantityAllocationDto> Allocations,
    decimal? SettledAmount = null, string? SettledCurrency = null);
