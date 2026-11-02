namespace InvestmentLab.Taxation.Contracts;

public sealed record SimulatedTradeDto(Guid InstrumentId, DateOnly On, bool IsSale, decimal Quantity, decimal Price, decimal Fee, string Currency);
